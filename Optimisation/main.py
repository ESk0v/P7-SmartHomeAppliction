from fastapi import FastAPI

app = FastAPI(title="Optimisation")

@app.get("/run")
def hello():
    return {"message": "Hello from Python"}