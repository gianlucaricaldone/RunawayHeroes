#!/bin/bash

echo "PDF to Markdown Converter (con supporto tabelle tramite Tabula)"
echo "========================================================="
echo

# Verifica se Python è installato
if ! command -v python3 &> /dev/null; then
    echo "Errore: Python3 non è installato."
    exit 1
fi

# Verifica se Java è installato (necessario per Tabula)
if ! command -v java &> /dev/null; then
    echo "Errore: Java non è installato (richiesto da Tabula)."
    echo "Installalo con il gestore pacchetti del tuo sistema."
    exit 1
fi

# Verifica o installa le dipendenze Python necessarie
python3 -c "
import sys
try:
    import tabula
    import pandas as pd
    import pdfplumber
    import markdown
except ImportError:
    print('Installazione delle dipendenze necessarie...')
    import subprocess
    subprocess.check_call([sys.executable, '-m', 'pip', 'install', 'tabula-py', 'pandas', 'pdfplumber', 'markdown'])
    import tabula
    import pandas as pd
    import pdfplumber
    import markdown
"

# Richiedi la cartella di input
read -p "Inserisci il percorso della cartella con i PDF (o premi Enter per usare la cartella corrente): " input_dir
input_dir=${input_dir:-$(pwd)}

# Crea una cartella di output per i file Markdown
output_dir="${input_dir}/markdown_output"
mkdir -p "$output_dir"

echo
echo "Verranno convertiti tutti i PDF in: $input_dir"
echo "I file Markdown verranno salvati in: $output_dir"
echo
echo "Conversione in corso..."

# Script Python per la conversione
python3 - << EOF
import os
import sys
import re
import pandas as pd
import tabula
import pdfplumber
from pathlib import Path

input_dir = "$input_dir"
output_dir = "$output_dir"

success_count = 0
fail_count = 0

def dataframe_to_markdown(df):
    """Converte un DataFrame pandas in una tabella Markdown."""
    markdown_table = []
    # Aggiungi intestazioni
    headers = "| " + " | ".join(str(col) for col in df.columns) + " |"
    markdown_table.append(headers)
    # Aggiungi separatore
    separator = "| " + " | ".join(["---"] * len(df.columns)) + " |"
    markdown_table.append(separator)
    # Aggiungi righe
    for _, row in df.iterrows():
        values = [str(val).replace("\n", " ").strip() for val in row.values]
        row_str = "| " + " | ".join(values) + " |"
        markdown_table.append(row_str)
    return "\n".join(markdown_table)

# Cerca ricorsivamente tutti i file PDF
for root, dirs, files in os.walk(input_dir):
    for file in files:
        if file.lower().endswith(".pdf"):
            try:
                pdf_path = os.path.join(root, file)
                rel_path = os.path.relpath(root, input_dir)
                filename = os.path.splitext(file)[0]
                
                # Crea la struttura delle sottodirectory
                if rel_path != ".":
                    out_dir = os.path.join(output_dir, rel_path)
                    os.makedirs(out_dir, exist_ok=True)
                    out_file = os.path.join(out_dir, filename + ".md")
                else:
                    out_file = os.path.join(output_dir, filename + ".md")
                
                print(f"Elaborazione: {file}")
                
                # Estrai testo normale con pdfplumber
                text_content = []
                with pdfplumber.open(pdf_path) as pdf:
                    for page_num, page in enumerate(pdf.pages):
                        page_text = page.extract_text() or ""
                        if page_text:
                            text_content.append(f"## Pagina {page_num + 1}\n\n{page_text}\n\n")
                
                # Estrai tabelle con tabula
                try:
                    tables = tabula.read_pdf(pdf_path, pages='all', multiple_tables=True)
                    table_content = []
                    
                    for i, table in enumerate(tables):
                        if not table.empty:
                            table_content.append(f"### Tabella {i+1}\n\n")
                            table_content.append(dataframe_to_markdown(table))
                            table_content.append("\n\n")
                except Exception as e:
                    print(f"Avviso: Errore nell'estrazione delle tabelle: {str(e)}")
                    tables = []
                    table_content = []
                
                # Combina testo e tabelle (semplificato - in una versione più avanzata si potrebbero posizionare le tabelle nel posto giusto)
                md_content = "".join(text_content) + "\n" + "".join(table_content)
                
                # Pulisci e formatta il testo Markdown
                # 1. Rimuovi linee vuote multiple
                md_content = re.sub(r'\n{3,}', '\n\n', md_content)
                
                # Salva il risultato come file Markdown
                with open(out_file, "w", encoding="utf-8") as f:
                    f.write(md_content)
                
                print(f"Convertito con successo: {out_file}")
                success_count += 1
                
            except Exception as e:
                print(f"Errore nella conversione di {file}: {str(e)}")
                fail_count += 1
            
            print()

print(f"Operazione completata!")
print(f"Convertiti con successo: {success_count} file")
print(f"Falliti: {fail_count} file")
print(f"I file MD sono disponibili in: {output_dir}")
EOF

echo "Script completato."