import os
import sys
import json
import asyncio

BASE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Assets")

TOOLS = [
    {
        "name": "listar_scripts",
        "description": "Lista scripts C# (.cs) y shaders en Assets/Scripts o subcarpetas.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "subcarpeta": {"type": "string", "default": "Scripts"}
            }
        }
    },
    {
        "name": "leer_script",
        "description": "Lee el contenido de un script dentro de Assets.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "ruta_relativa": {"type": "string"}
            },
            "required": ["ruta_relativa"]
        }
    },
    {
        "name": "guardar_script",
        "description": "Crea o sobreescribe un script dentro de Assets.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "ruta_relativa": {"type": "string"},
                "contenido": {"type": "string"}
            },
            "required": ["ruta_relativa", "contenido"]
        }
    }
]

def ejecutar_herramienta(name, args):
    if name == "listar_scripts":
        subcarpeta = args.get("subcarpeta", "Scripts")
        target_dir = os.path.normpath(os.path.join(BASE_DIR, subcarpeta))
        if not target_dir.startswith(BASE_DIR):
            return [{"type": "text", "text": "Error: Acceso fuera de Assets denegado."}]
        if not os.path.exists(target_dir):
            return [{"type": "text", "text": "Carpeta vacia o no encontrada."}]
        
        encontrados = []
        for raiz, _, archivos in os.walk(target_dir):
            for f in archivos:
                if f.endswith((".cs", ".shader", ".hlsl", ".json")):
                    encontrados.append(os.path.relpath(os.path.join(raiz, f), BASE_DIR))
        return [{"type": "text", "text": "\n".join(encontrados) if encontrados else "No hay scripts en la carpeta."}]

    elif name == "leer_script":
        ruta_rel = args.get("ruta_relativa", "")
        ruta_completa = os.path.normpath(os.path.join(BASE_DIR, ruta_rel))
        if not ruta_completa.startswith(BASE_DIR):
            return [{"type": "text", "text": "Error: Acceso fuera de Assets denegado."}]
        if not os.path.exists(ruta_completa):
            return [{"type": "text", "text": f"Error: El archivo {ruta_rel} no existe."}]
        with open(ruta_completa, "r", encoding="utf-8", errors="ignore") as f:
            return [{"type": "text", "text": f.read()}]

    elif name == "guardar_script":
        ruta_rel = args.get("ruta_relativa", "")
        contenido = args.get("contenido", "")
        ruta_completa = os.path.normpath(os.path.join(BASE_DIR, ruta_rel))
        if not ruta_completa.startswith(BASE_DIR):
            return [{"type": "text", "text": "Error: Acceso fuera de Assets denegado."}]
        os.makedirs(os.path.dirname(ruta_completa), exist_ok=True)
        with open(ruta_completa, "w", encoding="utf-8") as f:
            f.write(contenido)
        return [{"type": "text", "text": f"Script guardado exitosamente en Assets/{ruta_rel}."}]

    return [{"type": "text", "text": f"Herramienta desconocida: {name}"}]

async def main():
    loop = asyncio.get_event_loop()
    reader = asyncio.StreamReader()
    protocol = asyncio.StreamReaderProtocol(reader)
    await loop.connect_read_pipe(lambda: protocol, sys.stdin)

    while True:
        line = await reader.readline()
        if not line:
            break
        try:
            req = json.loads(line.decode("utf-8"))
        except Exception:
            continue

        req_id = req.get("id")
        method = req.get("method")
        params = req.get("params", {})

        response = {"jsonrpc": "2.0", "id": req_id}

        if method == "initialize":
            response["result"] = {
                "protocolVersion": "2024-11-05",
                "capabilities": {"tools": {}},
                "serverInfo": {"name": "NitroRhythm-Assistant", "version": "1.0.0"}
            }
        elif method == "notifications/initialized":
            continue
        elif method == "tools/list":
            response["result"] = {"tools": TOOLS}
        elif method == "tools/call":
            tool_name = params.get("name")
            tool_args = params.get("arguments", {})
            content = ejecutar_herramienta(tool_name, tool_args)
            response["result"] = {"content": content}
        else:
            response["error"] = {"code": -32601, "message": "Method not found"}

        if req_id is not None:
            sys.stdout.write(json.dumps(response) + "\n")
            sys.stdout.flush()

if __name__ == "__main__":
    asyncio.run(main())