from fastapi import FastAPI, HTTPException

from models import SolverReq
from optimiser import solve, InfeasibleError

app = FastAPI(title="Optimisation")

# plain def: FastAPI runs it in a worker thread, so the solver doesn't block the event loop
@app.post("/run")
def run(req: SolverReq):
    try:
        return solve(req)
    except InfeasibleError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))