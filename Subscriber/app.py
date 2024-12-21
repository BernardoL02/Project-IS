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
MQTT_BROKER = "localhost"       # Endereço do broker (Mosquitto)
MQTT_PORT = 1883                # Porta padrão MQTT
MQTT_TOPIC = "lamp/control"     # Tópico para controle da lâmpada

def create_application():
    xml_data = "<Application><Name>Lighting</Name></Application>"
    response = requests.post(BASE_URL, data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]

def create_container():
    xml_data = "<Container><Name>light_bulb</Name></Container>"
    response = requests.post(f"{BASE_URL}/Lighting", data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]

def create_notification():
    xml_data = """
    <Notification>
        <Name>state_change</Name>
        <Event>1</Event>
        <Endpoint>mqtt://localhost:1883</Endpoint>
        <Enabled>true</Enabled>
    </Notification>
    """
    response = requests.post(f"{BASE_URL}/Lighting/light_bulb", data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]

def setup_resources():
    """Cria os recursos automaticamente ao iniciar a aplicação."""
    create_application()
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

# Função para tratar mensagens recebidas via MQTT
def on_message(client, userdata, msg):
    global current_state
    payload = msg.payload.decode("utf-8")
    print(f"Mensagem recebida via MQTT: {payload}")

    if payload == "on":
        current_state = "on"
    elif payload == "off":
        current_state = "off"
    else:
        print(f"Mensagem desconhecida: {payload}")


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
