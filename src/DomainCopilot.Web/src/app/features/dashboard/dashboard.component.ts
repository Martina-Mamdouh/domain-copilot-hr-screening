import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScreeningService } from '../../core/services/screening.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  Math = Math; // Expose Math to template

  stats = {
    totalEvaluations: 0,
    aiShortlisted: 0,
    aiRejected: 0,
    managerApproved: 0,
    managerRejected: 0,
    managerOverrides: 0,
    pendingReview: 0,
    avgScore: 0
  };

  recentEvaluations: any[] = [];
  isLoading = true;
  errorMsg: string | null = null;

  // Review Modal State
  selectedEval: any = null;
  overrideReason: string = '';
  reviewComments: string = '';
  isReviewing = false;
  reviewError: string | null = null;
  
  // Edit State
  isEditMode = false;
  editedDecision: string = 'Approve'; // Approve, Hire, Shortlist, Reject
  editedScore: number | null = null;
  editedProbes: string = '';

  // Pagination State
  currentPage = 1;
  itemsPerPage = 5;

  constructor(private screeningService: ScreeningService, public authService: AuthService) { }

  ngOnInit() {
    this.loadEvaluations();
  }

  get paginatedEvaluations() {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.recentEvaluations.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages() {
    return Math.ceil(this.recentEvaluations.length / this.itemsPerPage);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  setPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  loadEvaluations() {
    this.isLoading = true;
    this.screeningService.getEvaluations().subscribe({
      next: (data) => {
        this.recentEvaluations = data;
        this.calculateStats(data);
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMsg = 'Failed to load evaluations.';
        this.isLoading = false;
      }
    });
  }

  calculateStats(data: any[]) {
    this.stats.totalEvaluations = data.length;

    // AI Stats
    this.stats.aiShortlisted = data.filter(e => e.recommendedDecision === 1 || e.recommendedDecision === 2 || e.recommendedDecision === 'Shortlist' || e.recommendedDecision === 'Hold').length;
    this.stats.aiRejected = data.filter(e => e.recommendedDecision === 3 || e.recommendedDecision === 'Reject').length;

    // Manager Stats
    this.stats.managerApproved = data.filter(e => e.status === 2 || e.status === 'Approved' || (e.status === 3 && e.recommendedDecision === 3)).length; // Status 3 (legacy override of reject = approved)
    this.stats.managerRejected = data.filter(e => e.status === 4 || e.status === 'Rejected' || (e.status === 3 && e.recommendedDecision === 1)).length; // Status 3 (legacy override of shortlist = rejected)

    this.stats.managerOverrides = data.filter(e =>
      e.status === 3 || e.status === 'Overridden' ||
      ((e.recommendedDecision === 1 || e.recommendedDecision === 'Shortlist') && (e.status === 4 || e.status === 'Rejected')) ||
      ((e.recommendedDecision === 3 || e.recommendedDecision === 'Reject') && (e.status === 2 || e.status === 'Approved'))
    ).length;

    this.stats.pendingReview = data.filter(e => e.status === 1 || e.status === 'PendingHumanApproval').length;

    if (data.length > 0) {
      const totalScore = data.reduce((acc, curr) => acc + (curr.weightedScore || 0), 0);
      this.stats.avgScore = Math.round(totalScore / data.length);
    }
  }

  getRecommendationLabel(rec: number | string): string {
    if (rec === 1 || rec === 'Shortlist') return 'Shortlist';
    if (rec === 2 || rec === 'Hold') return 'Hold';
    if (rec === 3 || rec === 'Reject') return 'Reject';
    return 'Unknown';
  }

  getStatusLabel(status: number | string): string {
    if (status === 1 || status === 'PendingHumanApproval') return 'Pending';
    if (status === 2 || status === 'Approved') return 'Approved';
    if (status === 3 || status === 'Overridden') return 'Overridden';
    if (status === 4 || status === 'Rejected') return 'Rejected';
    return 'Unknown';
  }

  openReviewModal(evaluation: any) {
    this.selectedEval = evaluation;
    this.overrideReason = '';
    this.reviewComments = '';
    this.reviewError = null;
    this.isEditMode = false;
    this.editedDecision = 'Change Decision To...';
    this.editedScore = evaluation.weightedScore;
    this.editedProbes = evaluation.interviewProbes || '';
  }

  closeReviewModal() {
    this.selectedEval = null;
  }

  toggleEditMode() {
    this.isEditMode = true;
  }

  submitReview(action: 'approve' | 'reject' | 'edit') {
    if (!this.selectedEval) return;

    this.reviewError = null;

    if (action === 'reject' && !this.overrideReason.trim()) {
      this.reviewError = 'An override reason is mandatory when rejecting.';
      return;
    }
    
    if (action === 'edit' && this.editedDecision === 'Change Decision To...') {
      this.reviewError = 'Please select a valid decision from the dropdown.';
      return;
    }

    if (action === 'edit' && !this.overrideReason.trim()) {
      this.reviewError = 'An override reason is mandatory when changing the decision or details.';
      return;
    }

    this.isReviewing = true;
    let finalStatus: number;
    let payload: any = {
      comments: this.reviewComments,
      overrideReason: this.overrideReason
    };

    if (action === 'approve') {
      finalStatus = (this.selectedEval.recommendedDecision === 3 || this.selectedEval.recommendedDecision === 'Reject') ? 4 : 2;
    } 
    else if (action === 'reject') {
      finalStatus = (this.selectedEval.recommendedDecision === 3 || this.selectedEval.recommendedDecision === 'Reject') ? 2 : 4;
    }
    else {
      // Edit & Approve
      if (this.editedDecision === 'Reject') finalStatus = 4;
      else finalStatus = 2; // Hire or Shortlist map to Approved
      
      payload.editedScore = this.editedScore;
      payload.editedProbes = this.editedProbes;
    }

    payload.finalStatus = finalStatus;

    this.screeningService.reviewEvaluation(this.selectedEval.id, payload).subscribe({
      next: () => {
        this.isReviewing = false;
        this.closeReviewModal();
        this.loadEvaluations(); // reload table
      },
      error: (err) => {
        this.isReviewing = false;
        this.reviewError = err.error || 'Failed to submit review.';
      }
    });
  }
}
