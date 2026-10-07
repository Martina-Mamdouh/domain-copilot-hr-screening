#!/bin/sh
echo "Waiting for API to be ready..."
sleep 5

for file in /corpus/CV-*.md; do
  echo "Ingesting $file..."
  curl -X POST -s -o /dev/null -w "Status: %{http_code}\n" -F "file=@$file" -F "documentType=Resume" -F "externalReferenceId=$(basename $file .md)" $API_URL/api/corpus/ingest
done

for file in /corpus/JD-*.md; do
  echo "Ingesting $file..."
  curl -X POST -s -o /dev/null -w "Status: %{http_code}\n" -F "file=@$file" -F "documentType=JobDescription" -F "externalReferenceId=$(basename $file .md)" $API_URL/api/corpus/ingest
done

for file in /corpus/RUB-*.md; do
  echo "Ingesting $file..."
  curl -X POST -s -o /dev/null -w "Status: %{http_code}\n" -F "file=@$file" -F "documentType=Rubric" -F "externalReferenceId=$(basename $file .md)" $API_URL/api/corpus/ingest
done

echo "Seeding completed."
