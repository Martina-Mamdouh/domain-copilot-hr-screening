import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

export interface IngestRequest {
  documentId: string;
  textContent: string;
  metadata?: any;
}

@Injectable({
  providedIn: 'root'
})
export class CorpusService {
  private apiUrl = `${environment.apiUrl}/retrieval`;

  constructor(private http: HttpClient) { }

  ingestDocument(request: IngestRequest): Observable<any> {
    const blob = new Blob([request.textContent], { type: 'text/plain' });
    const file = new File([blob], `${request.documentId}.txt`, { type: 'text/plain' });

    const formData = new FormData();
    formData.append('File', file);
    formData.append('DocumentType', 'JobDescription'); // Default to JD or Guideline
    formData.append('ExternalReferenceId', request.documentId);

    return this.http.post<any>(`${this.apiUrl}/ingest-document`, formData);
  }
}
