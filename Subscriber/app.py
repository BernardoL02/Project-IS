from flask import Flask, jsonify, render_template
import threading
import paho.mqtt.client as mqtt
import xmltodict

app = Flask(__name__)

# Variáveis globais
mqtt_host = "127.0.0.1"  # Host padrão
mqtt_topic = "mqtt://127.0.0.1"  # Tópico padrão
received_messages = []
client = None  # Cliente MQTT global
lock = threading.Lock()  # Lock para acesso seguro

# Callback MQTT para receber mensagens
def on_message(client, userdata, message):
    payload = message.payload.decode()
    print("Recebido:", payload)
    try:
        # Converter XML em JSON
        json_data = xmltodict.parse(payload)
        with lock:
            received_messages.append(json_data)  # Adicionar o JSON à lista
    except Exception as e:
        print("Erro ao processar mensagem XML:", e)
        with lock:
            received_messages.append({"error": "Mensagem inválida", "content": payload})

# Conectar ao broker MQTT
def connect_mqtt(host, topic):
    global client, received_messages
    received_messages = []  # Limpa mensagens ao reconectar
    print(f"Conectando ao MQTT broker em {host} no tópico {topic}...")
    client = mqtt.Client()
    client.on_message = on_message
    client.connect(host)
    client.subscribe(topic)
    client.loop_start()
    print("Conexão MQTT estabelecida!")

# Desconectar do broker MQTT
def disconnect_mqtt():
    global client
    if client:
        print("Desconectando do MQTT broker...")
        client.loop_stop()
        client.disconnect()
        client = None
        print("Desconectado do MQTT broker.")

# Rota para acessar mensagens recebidas
@app.route("/messages", methods=["GET"])
def get_messages():
    with lock:  # Garantir acesso seguro à lista
        return jsonify(received_messages)

# Página inicial
@app.route("/", methods=["GET"])
def index():
    return render_template("index.html")

# Configurar e conectar ao broker MQTT
@app.route("/configure/<host>/<topic>", methods=["GET"])
def configure_mqtt(host, topic):
    global mqtt_host, mqtt_topic
    print(f"Configurando MQTT com host: {host} e tópico: {topic}")
    mqtt_host, mqtt_topic = host, topic
    disconnect_mqtt()  # Desconectar se já estiver conectado
    connect_mqtt(mqtt_host, mqtt_topic)  # Reconectar
    return f"MQTT configurado e conectado ao host: {host}, tópico: {topic}"

# Rota para desconectar
@app.route("/disconnect", methods=["GET"])
def disconnect():
    disconnect_mqtt()
    return "Desconectado do broker MQTT."

# Thread para o cliente MQTT
def mqtt_listener():
    connect_mqtt(mqtt_host, mqtt_topic)

# Iniciar o Flask
if __name__ == "__main__":
    # Iniciar cliente MQTT em thread separada
    threading.Thread(target=mqtt_listener, daemon=True).start()
    # Rodar Flask na thread principal
    app.run(host="0.0.0.0", port=5000, debug=True)
