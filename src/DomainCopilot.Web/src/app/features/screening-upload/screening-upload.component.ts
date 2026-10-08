import { Component, OnInit } from '@angular/core';
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
export class ScreeningUploadComponent implements OnInit {
  // Mode selection
  isRawTextMode = false;

  // Raw text properties
  jobDescription: string = '';
  rawCvText: string = '';

  // File & dropdown properties
  availableJds: any[] = [];
  selectedJdId: string = '';
  selectedFile: File | null = null;
  uploadedCandidateDocId: string = '';
  
  isLoading = false;
  isUploading = false;
  result: ScreeningResult | null = null;
  error: string | null = null;

  constructor(private screeningService: ScreeningService) {}

  ngOnInit() {
    this.loadJds();
  }

  loadJds() {
    this.screeningService.getDocuments('JobDescription').subscribe({
      next: (jds) => {
        this.availableJds = jds;
        if (jds.length > 0) this.selectedJdId = jds[0].docId;
      },
      error: (err) => console.error('Failed to load JDs', err)
    });
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  submitScreening() {
    this.error = null;

    if (this.isRawTextMode) {
      if (!this.jobDescription || !this.rawCvText) {
        this.error = 'Please provide both JD and CV text.';
        return;
      }
      this.runEvaluation({ jobDescription: this.jobDescription, rawCvText: this.rawCvText });
    } else {
      if (!this.selectedJdId || !this.selectedFile) {
        this.error = 'Please select a JD and upload a CV file.';
        return;
      }

      this.isUploading = true;
      this.screeningService.ingestDocument(this.selectedFile, 'Resume').subscribe({
        next: (res) => {
          this.isUploading = false;
          this.uploadedCandidateDocId = res.docId;
          this.runEvaluation({ candidateDocId: this.uploadedCandidateDocId, targetJdId: this.selectedJdId });
        },
        error: (err) => {
          this.isUploading = false;
          this.error = 'Failed to upload CV: ' + (err.error || err.message);
        }
      });
    }
  }

  runEvaluation(req: ScreeningRequest) {
    this.isLoading = true;
    this.result = null;

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

  getRecommendationLabel(rec: number): string {
    if (rec === 1) return 'Shortlist';
    if (rec === 2) return 'Hold';
    if (rec === 3) return 'Reject';
    return 'Unknown';
  }
}
