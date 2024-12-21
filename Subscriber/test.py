from http.server import BaseHTTPRequestHandler, HTTPServer

# Porta onde o servidor ficará à escuta
PORT = 1884

class SimpleHTTPRequestHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        """Trata requisições HTTP POST"""
        content_length = int(self.headers['Content-Length'])  # Obtém o tamanho do corpo da mensagem
        post_data = self.rfile.read(content_length)  # Lê o conteúdo da mensagem
        print("Recebido POST:")
        print(post_data.decode('utf-8'))  # Exibe o conteúdo recebido no console

        # Envia uma resposta de volta
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()
        self.wfile.write(b"Recebido com sucesso!")

    def do_GET(self):
        """Trata requisições HTTP GET (opcional)"""
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()
        self.wfile.write(b"Servidor ativo! Use POST para enviar dados.")

def run():
    print(f"Servidor a escutar na porta {PORT}")
    server_address = ('', PORT)
    httpd = HTTPServer(server_address, SimpleHTTPRequestHandler)
    httpd.serve_forever()

if __name__ == "__main__":
    run()
