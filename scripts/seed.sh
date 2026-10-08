#!/bin/sh
echo "Waiting for API to be ready..."
sleep 5

echo "Logging in as admin to get token..."
TOKEN=$(curl -s -X POST -H "Content-Type: application/json" -d '{"email":"admin@copilot.local","password":"Admin@123456"}' $API_URL/api/auth/login | grep -o '"token":"[^"]*' | grep -o '[^"]*$')

if [ -z "$TOKEN" ]; then
  echo "Failed to get token! Please ensure the API is running and seeded."
  exit 1
fi
echo "Got token."

for file in /corpus/CV-001.md /corpus/CV-002.md; do
  echo "Ingesting $file..."
  curl -X POST -s -o /dev/null -w "Status: %{http_code}\n" -H "Authorization: Bearer $TOKEN" -F "file=@$file" -F "documentType=Resume" -F "externalReferenceId=$(basename $file .md)" $API_URL/api/retrieval/ingest-document
  sleep 5
done

for file in /corpus/JD-001.md /corpus/JD-002.md; do
  echo "Ingesting $file..."
  curl -X POST -s -o /dev/null -w "Status: %{http_code}\n" -H "Authorization: Bearer $TOKEN" -F "file=@$file" -F "documentType=JobDescription" -F "externalReferenceId=$(basename $file .md)" $API_URL/api/retrieval/ingest-document
  sleep 5
done

for file in /corpus/RUB-001.md /corpus/RUB-002.md; do
  echo "Ingesting $file..."
  curl -X POST -s -o /dev/null -w "Status: %{http_code}\n" -H "Authorization: Bearer $TOKEN" -F "file=@$file" -F "documentType=Rubric" -F "externalReferenceId=$(basename $file .md)" $API_URL/api/retrieval/ingest-document
  sleep 5
done

echo "Seeding completed."
