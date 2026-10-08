import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CopilotService, GroundedAnswerDto } from '../../core/services/copilot.service';

interface ChatMessage {
  text: string;
  isBot: boolean;
  isError?: boolean;
  sources?: any[];
}

@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './copilot-chat.component.html',
  styleUrl: './copilot-chat.component.css'
})
export class CopilotChatComponent {
  query: string = '';
  messages: ChatMessage[] = [
    { text: 'Hello! I am Domain Copilot. You can ask me anything about internal HR policies or evaluation guidelines. I will cite my sources.', isBot: true }
  ];
  isLoading = false;

  constructor(private copilotService: CopilotService) {}

  ask() {
    if (!this.query.trim()) return;
    
    const userQuery = this.query.trim();
    this.messages.push({ text: userQuery, isBot: false });
    this.query = '';
    this.isLoading = true;

    this.copilotService.askQuery({ query: userQuery, topK: 3 }).subscribe({
      next: (res: GroundedAnswerDto) => {
        this.messages.push({
          text: res.answer,
          isBot: true,
          sources: res.sources
        });
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.messages.push({
          text: 'Error communicating with the Copilot API.',
          isBot: true,
          isError: true
        });
        this.isLoading = false;
      }
    });
  }
}
