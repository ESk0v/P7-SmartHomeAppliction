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
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI()

class Item(BaseModel):
    text: str
    is_done: bool = False

items = []

@app.get("/")
def read_root():
    return {'message': 'Hello FastAPI'}

# med en post request kan dette endpoint rammes og create_item køres
@app.post("/items")
def create_item(item: Item):
    items.append(item)
    return items


@app.get("/items_true")
def list_items_true(limit: int) -> list[Item]:
    completed_items = [item for item in items if item.is_done]
    return completed_items[0:limit]


@app.get("/items_all")
def list_items_all(limit: int):
    return items[0:limit]


@app.get("/items/{item_id}")
def get_item(item_id: int) -> Item:
    if item_id < len(items):
        return items[item_id]
    else:
        raise HTTPException(status_code=404, detail="Item not found")


