from flask import Flask, render_template, request
import requests
import xml.etree.ElementTree as ET
import threading
import paho.mqtt.client as mqtt
import urllib3

# Suprimir avisos de HTTPS não verificados
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

app = Flask(__name__)

BASE_URL = "https://localhost:44322/api/somiod"
HEADERS = {"Content-Type": "application/xml"}

# Variável para armazenar o estado atual da lâmpada
current_state = None

# Configurações do broker MQTT
MQTT_BROKER = "localhost"  # Endereço do broker (Mosquitto)
MQTT_PORT = 1883           # Porta padrão MQTT
MQTT_TOPIC = "api/somiod/Lighting/light_bulb"  # Tópico para controle da lâmpada

def check_application_exists(application_name):
    """Verifica se a aplicação já existe."""
    response = requests.get(f"{BASE_URL}/{application_name}", headers=HEADERS, verify=False)
    if response.status_code == 200:
        root = ET.fromstring(response.content)
        name = root.find("Name").text
        if name == application_name:
            return True
    return False

def create_application():
    """Cria a aplicação se ela ainda não existir."""
    application_name = "Lighting"
    if check_application_exists(application_name):
        print(f"A aplicacao '{application_name}' ja existente.")
        return True

    xml_data = f"<Application><Name>{application_name}</Name></Application>"
    response = requests.post(BASE_URL, data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]

def check_container_exists(application_name, container_name):
    """Verifica se o container já existe dentro de uma aplicação."""
    response = requests.get(f"{BASE_URL}/{application_name}/{container_name}", headers=HEADERS, verify=False)
    if response.status_code == 200:
        root = ET.fromstring(response.content)
        name = root.find("Name").text
        if name == container_name:
            return True
    return False

def create_container():
    """Cria o container se ele ainda não existir."""
    application_name = "Lighting"
    container_name = "light_bulb"
    if check_container_exists(application_name, container_name):
        print(f"O container '{container_name}' ja existe na aplicacao '{application_name}'.")
        return True

    xml_data = f"<Container><Name>{container_name}</Name></Container>"
    response = requests.post(f"{BASE_URL}/{application_name}", data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]



def check_notification_exists(application_name, container_name, notification_name):
    """Verifica se a notificação já existe dentro de um container."""
    response = requests.get(f"{BASE_URL}/{application_name}/{container_name}/notification/{notification_name}", headers=HEADERS, verify=False)
    if response.status_code == 200:
        root = ET.fromstring(response.content)
        name = root.find("Name").text
        if name == notification_name:
            return True
    return False

def create_notification():
    """Cria a notificação se ela ainda não existir."""
    application_name = "Lighting"
    container_name = "light_bulb"
    notification_name = "state_change"
    if check_notification_exists(application_name, container_name, notification_name):
        print(f"A notificacao '{notification_name}' ja existe no container '{container_name}'.")
        return True

    xml_data = f"""
    <Notification>
        <Name>{notification_name}</Name>
        <Event>1</Event>
        <Endpoint>mqtt://localhost:1883</Endpoint>
        <Enabled>true</Enabled>
    </Notification>
    """
    response = requests.post(f"{BASE_URL}/{application_name}/{container_name}", data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]



def setup_resources():
    """Cria os recursos automaticamente ao iniciar a aplicação."""
    if create_application():
        create_container()
        create_notification()

@app.route("/", methods=["GET"])
def index():
    global current_state
    return render_template("index.html", message=None, state=current_state)

# Rota para receber notificações e atualizar o estado da lâmpada
@app.route("/notify", methods=["POST"])
def notify():
    global current_state

    # Parseia o XML recebido para obter o estado
    data = request.data
    root = ET.fromstring(data)
    content = root.find("Content").text

    if content == "on":
        current_state = "on"
    elif content == "off":
        current_state = "off"

    return "Notification received", 200

@app.route("/state", methods=["GET"])
def get_state():
    global current_state
    return {"state": current_state}, 200  # Retorna o estado como JSON


# Função para tratar mensagens recebidas via MQTT
def on_message(client, userdata, msg):
    global current_state
    payload = msg.payload.decode("utf-8")
    print(f"Mensagem recebida via MQTT: {payload}")

    try:
        # Parsear o XML recebido
        root = ET.fromstring(payload)
        record = root.find("Record")
        if record is not None:
            content = record.find("Content").text
            if content == "on":
                current_state = "on"
            elif content == "off":
                current_state = "off"
            print(f"Estado atualizado: {current_state}")
        else:
            print("Formato de mensagem inesperado. Nenhum 'Record' encontrado.")
    except ET.ParseError as e:
        print(f"Erro ao parsear a mensagem XML: {e}")

# Configuração do cliente MQTT em uma thread separada
def mqtt_thread():
    client = mqtt.Client()
    client.on_message = on_message
    try:
        client.connect(MQTT_BROKER, MQTT_PORT, 60)
        client.subscribe(MQTT_TOPIC)
        print(f"Inscrito no tópico: {MQTT_TOPIC}")
        client.loop_forever()
    except Exception as e:
        print(f"Erro ao conectar ao broker MQTT: {e}")

if __name__ == "__main__":
    setup_resources()
    mqtt_listener = threading.Thread(target=mqtt_thread)
    mqtt_listener.daemon = True
    mqtt_listener.start()
    app.run(debug=True)
