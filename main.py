from fastapi import FastAPI, HTTPException
from db import get_connection

app = FastAPI(title="API con FastAPI y MySQL")

@app.get("/carta")
def obtener_cartas():
    conn = get_connection()
    if conn is None:
        raise HTTPException(status_code=500,detail="Error de conexión a la base de datos")
    try:
        cursor = conn.cursor(dictionary=True)
        cursor.execute("SELECT * FROM carta")
        resultados = cursor.fetchall()
        return {"carta": resultados}
    except Exception as e:
        raise HTTPException(status_code=500,detail=f"Error al consultar: {e}")
    finally:
        if conn.is_connected():
            cursor.close()
            conn.close()