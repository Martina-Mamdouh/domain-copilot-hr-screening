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
    return this.http.post<any>(`${this.apiUrl}/ingest-document`, request);
  }
}
