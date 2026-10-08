import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

export interface AskQuery {
  query: string;
  topK?: number;
}

export interface RetrievedChunkDto {
  documentId: string;
  chunkId: string;
  sectionTitle: string;
  text: string;
  confidenceScore: number;
}

export interface GroundedAnswerDto {
  answer: string;
  sources: RetrievedChunkDto[];
  isGrounded: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CopilotService {
  private apiUrl = `${environment.apiUrl}/retrieval`;

  constructor(private http: HttpClient) { }

  askQuery(request: AskQuery): Observable<GroundedAnswerDto> {
    return this.http.post<GroundedAnswerDto>(`${this.apiUrl}/ask`, request);
  }
}
