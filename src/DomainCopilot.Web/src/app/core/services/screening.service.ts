import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

export interface ScreeningRequest {
  jobDescription: string;
  rawCvText: string;
}

export interface ScreeningResult {
  agentOne: { extractedSkills: string, meetsMinimumRequirements: boolean };
  agentTwo: { sanitizedCv: string, injectionDetected: boolean };
  agentThree: { score: number, recommendation: string, reasoning: string, evidence?: string, sources?: any[] };
  traces: any[];
}

@Injectable({
  providedIn: 'root'
})
export class ScreeningService {
  private apiUrl = `${environment.apiUrl}/screening`;

  constructor(private http: HttpClient) { }

  evaluate(request: ScreeningRequest): Observable<ScreeningResult> {
    return this.http.post<ScreeningResult>(`${this.apiUrl}/evaluate`, request);
  }

  getEvaluations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/evaluations`);
  }
}
