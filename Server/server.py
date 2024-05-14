from http.server import HTTPServer, BaseHTTPRequestHandler
import os
import json

mesh_folder = R"C:\Users\luca-\OneDrive\Data\progetti\Borsa\python_scripts\meshes_2"

class FileServer(BaseHTTPRequestHandler):
    def do_GET(self):
        try:
            # Ottieni il percorso completo del file richiesto
            file_path = os.path.join(mesh_folder, self.path[1:])
            
            #controlla se il file esiste
            if not os.path.exists(file_path):
                raise FileNotFoundError
            # Apri il file in modalità binaria per leggerlo
            with open(file_path, 'rb') as file:
                # Leggi il contenuto del file
                content = file.read()
                # Imposta lo status della risposta a 200 (OK)
                self.send_response(200)
                # Imposta l'intestazione Content-type in base al tipo di file
                self.send_header('Content-type', 'application/octet-stream')
                # Aggiungi l'intestazione Content-length per indicare la lunghezza del contenuto
                self.send_header('Content-length', len(content))
                # Invia una riga vuota per separare le intestazioni dal corpo della risposta
                self.end_headers()
                # Invia il contenuto del file
                self.wfile.write(content)
        except FileNotFoundError:
            # Se il file non esiste, invia una risposta 404 (Not Found)
            self.send_error(404,"File not found: %s" % self.path)

        

def run(port=8080):
    server_address = ('', port)
    httpd = HTTPServer(server_address, FileServer)
    print('Server running at localhost:%s' % port)
    httpd.serve_forever()

def get_file_list():
    print('Getting file list...')
    json_path = os.path.join(mesh_folder, 'mesh_list.json')
    
    file_info_list = []

    for file_name in os.listdir(mesh_folder):
        if file_name.endswith('.drc'):
            stat = os.stat(os.path.join(mesh_folder, file_name))
            file_info = {
                'name': file_name,
                'size': stat.st_size,
                'LOD' : 0
            }
            file_info_list.append(file_info)
    with open(json_path, 'w') as json_file:
        json.dump(file_info_list, json_file, indent=4)
    print('File list saved to %s' % json_path)


if __name__ == '__main__':
    get_file_list()
    run()