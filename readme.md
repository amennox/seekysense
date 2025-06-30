# 🥅 Obiettivo

Prototipo di motore MCP di una rag semi dinamica.

---

## 🛠️ Motore di Rendering

Effettua il rendering di un template testuale con dati dinamici tramite **RazorEngine**.

🔗 Endpoint per testare il rendering: [`/render`](https://localhost:7088/render)

---

## 📝 Esempio di Template

```jsonc
{
  // Template con struttura Razor-like per descrizione prodotto
  "template": "Il prodotto @Model.Name è descritto come: @Model.Description.\n\n@Model.Name fa parte dei reparti:\n@foreach (var reparto in Model.Departments) {\n    <text>- @reparto</text>\n}",

  // Dati forniti al template
  "data": {
    "Name": "SuperPhone 3000",
    "Description": "uno smartphone avanzato con AI",
    "Departments": [
      "Elettronica",
      "Tecnologia"
    ],
    "Promotions": [
      "Sconto primavera",
      "Cashback"
    ],
    "Price": 599.99
  }
}
```

**Risultato atteso**
```
Il prodotto SuperPhone 3000 è descritto come: uno smartphone avanzato con AI.

SuperPhone 3000 fa parte dei reparti:
- Elettronica
- Tecnologia
``` 

## Indici Elastic
```
PUT /ftelements
{
  "mappings": {
    "properties": {
      "question": {
        "type": "text",
        "analyzer": "standard"
      },
      "answer": {
        "type": "text",
        "analyzer": "standard"
      },
      "scope": {
        "type": "keyword"
      },
      "isPositive": {
        "type": "boolean"
      },
      "dateTime": {
        "type": "date",
        "format": "strict_date_optional_time||epoch_millis"
      },
      "reference": {
        "type": "text",
        "analyzer": "standard"
      },
      "source": {
        "type": "keyword"
      },
      "businessId": {
        "type": "keyword"
      }
  }
}
}
```

```
PUT /elements
{
  "elements": {
    "mappings": {
      "properties": {
        "businessId": {
          "type": "text"
        },
        "commands": {
          "type": "nested",
          "properties": {
            "commandName": {
              "type": "text"
            },
            "commandUrl": {
              "type": "text"
            }
          }
        },
        "fulltext": {
          "type": "text"
        },
        "fulltextVect": {
          "type": "dense_vector",
          "dims": 1024,
          "index": true,
          "similarity": "cosine"
        },
        "fulltextVectFT": {
          "type": "dense_vector",
          "dims": 1024,
          "index": true,
          "similarity": "cosine"
        },
        "id": {
          "type": "text"
        },
        "liveDataTemplate": {
          "type": "text"
        },
        "liveDataUrl": {
          "type": "text"
        },
        "liveDataValidation": {
          "type": "text"
        },
        "scope": {
          "type": "keyword"
        },
        "title": {
          "type": "text"
        }
      }
    }
  }
}
```

```
PUT /snapshot
{
  "mappings": {
    "properties": {
      "title": {
        "type": "text"
      },
      "scope": {
        "type": "keyword"
      },
      "businessid": {
        "type": "keyword"
      },
      "fulltext": {
        "type": "text"
      },
      "fulltextvect": {
        "type": "dense_vector",
        "dims": 1024,
        "index": true,         
        "similarity": "cosine"
      },
      "imageurl": {
        "type": "keyword"
      },
      "imagevect": {
        "type": "dense_vector",
        "dims": 512,         
        "index": true,
        "similarity": "cosine"
      },
      "elementid": {
        "type": "keyword"
      }
    }
  }
}

```

```
PUT /ftimages
{
  "mappings": {
    "properties": {
      "businessId": {
        "type": "keyword"
      },
      "scope": {
        "type": "keyword"
      },
      "image": {
        "type": "keyword"
      },
      "description": {
        "type": "text",
        "analyzer": "standard"
      }
    }
  }
}
```

## FINE TUNING VIDEO
 - 1 Fornire una lista di filmati youtube
 - 2 Download e processo del video
   - Estrazione dei frame ogni 2 secondi con scarto di quelli poco diversi
   - creazione della descrizione dello snapshot per il file tuning
   - salvataggio file e json con coppia immagine/descrizione
 - 3 lanciare addestramento con dataset consolidati e salvare modello
 - 4 rendere disponibile il motore di embedding per indicizzare i filmati

## FINE TUNUNIG IMMAGINI - UN FT multimodale per OGNI SCOPE
  - 1 Collezionare una directory di immagini su disco
  - 2 Lanciare uno script che importi le immagini in "ftimages" per uno Scope fornendo una descrizione (usando ad esempio ollama). Ecript esempio:EmbProduct\ftimg.py
  - 3 Lanciare da interfaccia il fine tuning delle immagine di quello scope (deve essere attivo il servizio main.py su porta 8000 del progetto VideoGuide)  

## FINE TUNING TESTO - UN FT testuale per TUTTI gli SCOPE
  - 1 Creare una tripleta di frasi (positive, neutre e negative) e inserirle in /ftelements attribuendo lo Scope (per ora però FT testuale è globale)
  - 2 Lanciare a riga di comando l'addestramento (per 8.000 elementi circa 24 ore), EmbFineTuning\Training.py (salva i checkpoint parziali)
  - 3 Rendere disponibile il motore lanciando embedding_server.py (emula un ollama sulla porta 11435)
