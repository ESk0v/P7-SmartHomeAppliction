"""
For at lave venv
    python 3 -m venv .venv

For at aktivere
    source .venv/bin/activate

For at installere requirements.txt libs
    pip install -r requirements.txt

For at deaktivere (gør kun når man er færdig med at arbejde i python terminalen)
    deactivate

Hvis den ikke kan genkende imports 
    Vælg den rigtige intepreter nede i højre hjørne. Default er 3.15.1 men i virtual environment er den .venv (3.15.1)

Kør FastAPI med command
    uvicorn main:app --reload --port 6767
        --reload gør at siden reloader på python file save og --port gør at man kan pin en port
    http://localhost:6767/docs giver en komplet Swagger UI til at teste den API med (INSANE)
"""
from fastapi import FastAPI

app = FastAPI()

items = []

@app.get("/")
def read_root():
    return {'message': 'Hello FastAPI'}

@app.post("/items")
def create_item(item):
    items.append(item)
    return items

