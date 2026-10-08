import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CorpusService, IngestRequest } from '../../core/services/corpus.service';

@Component({
  selector: 'app-corpus-ingestion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './corpus-ingestion.component.html',
  styleUrl: './corpus-ingestion.component.css'
})
export class CorpusIngestionComponent {
  documentId: string = '';
  textContent: string = '';
  
  isLoading = false;
  successMsg: string | null = null;
  errorMsg: string | null = null;

  constructor(private corpusService: CorpusService) {}

  submitIngestion() {
    if (!this.documentId || !this.textContent) return;
    
    this.isLoading = true;
    this.successMsg = null;
    this.errorMsg = null;

    const req: IngestRequest = {
      documentId: this.documentId,
      textContent: this.textContent
    };

    this.corpusService.ingestDocument(req).subscribe({
      next: (res) => {
        this.successMsg = 'Document ingested successfully into the Vector Database!';
        this.isLoading = false;
        this.documentId = '';
        this.textContent = '';
      },
      error: (err) => {
        console.error(err);
        this.errorMsg = 'Failed to ingest document.';
        this.isLoading = false;
      }
    });
  }
}
