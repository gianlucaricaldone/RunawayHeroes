#!/bin/bash

# Definisci la directory di partenza (. per la directory corrente)
DIRECTORY="."
# Definisci il file di output
OUTPUT_FILE="unity_folder_structure.txt"

# Verifica se il comando find è disponibile
if ! command -v find &> /dev/null; then
    echo "Errore: il comando 'find' non è installato."
    exit 1
fi

# Crea il file di output
echo "Alberatura delle directory, file .cs, .png e .fbx a partire da: $(readlink -f $DIRECTORY)" > "$OUTPUT_FILE"
echo "Generato il: $(date)" >> "$OUTPUT_FILE"
echo "----------------------------------------" >> "$OUTPUT_FILE"

# Prima raccogli tutte le directory
find "$DIRECTORY" -type d | sort > /tmp/dirs.tmp

# Poi raccogli tutti i file con le estensioni specificate
find "$DIRECTORY" -type f \( -name "*.cs" -o -name "*.png" -o -name "*.fbx" \) | sort > /tmp/files.tmp

# Combina i due elenchi
cat /tmp/dirs.tmp /tmp/files.tmp | sort | while read item; do
    # Calcola il livello di indentazione contando gli slash
    indent=$(echo "$item" | sed 's/[^\/]//g' | wc -c)
    # Sottrai 1 dall'indentazione se la directory di partenza è "."
    if [ "$DIRECTORY" = "." ]; then
        indent=$((indent - 1))
    fi
    
    # Crea l'indentazione appropriata
    padding=""
    for ((i=0; i<indent; i++)); do
        padding="${padding}  "
    done
    
    # Ottieni il nome base dell'elemento
    basename=$(basename "$item")
    
    # Aggiungi un indicatore per le directory e i file specificati
    if [ -d "$item" ]; then
        echo "${padding}📁 $basename" >> "$OUTPUT_FILE"
    elif [[ "$item" == *.cs ]]; then
        echo "${padding}📄 $basename" >> "$OUTPUT_FILE"
    elif [[ "$item" == *.png ]]; then
        echo "${padding}🖼️ $basename" >> "$OUTPUT_FILE"
    elif [[ "$item" == *.fbx ]]; then
        echo "${padding}🧊 $basename" >> "$OUTPUT_FILE"
    fi
done

# Rimuovi i file temporanei
rm -f /tmp/dirs.tmp /tmp/files.tmp

echo "----------------------------------------" >> "$OUTPUT_FILE"
echo "Alberatura salvata nel file $OUTPUT_FILE"