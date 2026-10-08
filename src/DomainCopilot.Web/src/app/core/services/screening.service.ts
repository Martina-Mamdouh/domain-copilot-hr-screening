import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ScreeningRequest {
  candidateDocId?: string;
  targetJdId?: string;
  rawCvText?: string;
  jobDescription?: string;
}

export interface ScreeningResult {
  id: string;
  candidateAlias: string;
  weightedScore: number;
  recommendedDecision: number;
  status: number;
  competencyBreakdownJson: string;
  traces: any[];
}

@Injectable({
  providedIn: 'root'
})
export class ScreeningService {
  private apiUrl = `${environment.apiUrl}/screening`;

  constructor(private http: HttpClient) { }

  getDocuments(category?: string): Observable<any[]> {
    let url = `${environment.apiUrl}/retrieval/documents`;
    if (category) {
      url += `?category=${category}`;
    }
    return this.http.get<any[]>(url);
  }

  ingestDocument(file: File, documentType: string): Observable<any> {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('DocumentType', documentType);
    const docId = `DOC-${Date.now()}`;
    formData.append('ExternalReferenceId', docId);

    return this.http.post<any>(`${environment.apiUrl}/retrieval/ingest-document`, formData).pipe(
      map(res => {
        return { ...res, docId };
      })
    );
  }

  evaluate(request: ScreeningRequest): Observable<ScreeningResult> {
    return this.http.post<ScreeningResult>(`${this.apiUrl}/evaluate`, request);
  }

  getEvaluations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/evaluations`);
  }

  reviewEvaluation(id: string, payload: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}s/${id}/review`, payload);
  }
}
