import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScreeningService, ScreeningRequest, ScreeningResult } from '../../core/services/screening.service';

@Component({
  selector: 'app-screening-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './screening-upload.component.html',
  styleUrl: './screening-upload.component.css'
})
export class ScreeningUploadComponent {
  jobDescription: string = '';
  rawCvText: string = '';
  
  isLoading = false;
  result: ScreeningResult | null = null;
  error: string | null = null;

  constructor(private screeningService: ScreeningService) {}

  submitScreening() {
    if (!this.jobDescription || !this.rawCvText) return;
    
    this.isLoading = true;
    this.result = null;
    this.error = null;

    const req: ScreeningRequest = {
      jobDescription: this.jobDescription,
      rawCvText: this.rawCvText
    };

    this.screeningService.evaluate(req).subscribe({
      next: (res) => {
        this.result = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Failed to run screening pipeline. Please check the backend.';
        this.isLoading = false;
      }
    });
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'var(--success)';
    if (score >= 60) return 'var(--warning)';
    return 'var(--danger)';
  }
}
