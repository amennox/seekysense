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

##Indici Elastic
```
PUT /semantic_source
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
      "is_positive": {
        "type": "boolean"
      },
      "datetime": {
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