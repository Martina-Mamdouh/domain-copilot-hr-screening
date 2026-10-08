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
  stats = {
    totalEvaluations: 0,
    shortlisted: 0,
    rejected: 0,
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

  constructor(private screeningService: ScreeningService, public authService: AuthService) {}

  ngOnInit() {
    this.loadEvaluations();
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
    this.stats.shortlisted = data.filter(e => e.recommendedDecision === 1 || e.recommendedDecision === 2 || e.recommendedDecision === 'Shortlist' || e.recommendedDecision === 'Hold').length;
    this.stats.rejected = data.filter(e => e.recommendedDecision === 3 || e.recommendedDecision === 'Reject').length; 
    
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
  }

  closeReviewModal() {
    this.selectedEval = null;
  }

  submitReview(isApproved: boolean) {
    if (!this.selectedEval) return;
    
    if (!isApproved && !this.overrideReason.trim()) {
      this.reviewError = 'An override reason is mandatory when rejecting/overriding.';
      return;
    }

    this.isReviewing = true;
    const finalStatus = isApproved ? 2 : 3; // 2 = Approved, 3 = Overridden

    const payload = {
      finalStatus: finalStatus,
      comments: this.reviewComments,
      overrideReason: this.overrideReason
    };

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
