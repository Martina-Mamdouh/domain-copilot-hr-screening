import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScreeningService } from '../../core/services/screening.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
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

  constructor(private screeningService: ScreeningService) {}

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
    this.stats.shortlisted = data.filter(e => e.recommendation === 1 || e.recommendation === 2).length; // Assuming enum values for Hire/Shortlist
    this.stats.rejected = data.filter(e => e.recommendation === 0).length; // Reject
    
    if (data.length > 0) {
      const totalScore = data.reduce((acc, curr) => acc + curr.score, 0);
      this.stats.avgScore = Math.round(totalScore / data.length);
    }
  }

  getRecommendationLabel(rec: number | string): string {
    if (rec === 0 || rec === 'Reject') return 'Reject';
    if (rec === 1 || rec === 'Shortlist') return 'Shortlist';
    if (rec === 2 || rec === 'Hire') return 'Hire';
    return 'Unknown';
  }

  getStatusLabel(status: number | string): string {
    if (status === 0 || status === 'Pending') return 'Pending';
    if (status === 1 || status === 'Approved') return 'Approved';
    if (status === 2 || status === 'Rejected') return 'Rejected';
    if (status === 3 || status === 'Overridden') return 'Overridden';
    return 'Unknown';
  }
}
