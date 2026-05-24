import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { DocumentQaApiError, DocumentQaService } from './core/api/document-qa.service';
import { RuntimeConfigService } from './core/config/runtime-config.service';
import { DocumentAskRequest, DocumentAskResponse, DocumentAskUsage } from './core/api/document-qa.models';

type ConversationState = 'loading' | 'success' | 'error';

interface ResultEntry {
  question: string;
  state: ConversationState;
  answer?: string;
  metadata?: Pick<DocumentAskResponse, 'ProcessingTimeMs' | 'Usage'>;
  errorTitle?: string;
  errorMessage?: string;
}

@Component({
  selector: 'app-root',
  imports: [ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly documentQaService = inject(DocumentQaService);
  private readonly runtimeConfig = inject(RuntimeConfigService);

  protected readonly title = 'Docquery Web MVP';
  protected readonly isSubmitting = signal(false);
  protected readonly latestResult = signal<ResultEntry | null>(null);
  protected readonly documentForm = new FormGroup({
    documentText: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/\S/)],
    }),
    question: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/\S/)],
    }),
  });

  protected get documentTextControl(): FormControl<string> {
    return this.documentForm.controls.documentText;
  }

  protected get questionControl(): FormControl<string> {
    return this.documentForm.controls.question;
  }

  protected get documentMetrics(): { characters: number; words: number } {
    const text = this.documentTextControl.value.trim();
    const wordCount = text.length === 0 ? 0 : text.split(/\s+/).filter(Boolean).length;

    return {
      characters: text.length,
      words: wordCount,
    };
  }

  protected get questionCharacterCount(): number {
    return this.questionControl.value.trim().length;
  }

  protected get apiLabel(): string {
    return 'Configured API';
  }

  protected get apiBaseUrl(): string {
    return this.runtimeConfig.apiBaseUrl ?? 'Runtime config pending';
  }

  protected get canSubmit(): boolean {
    return this.documentForm.valid && !this.isSubmitting();
  }

  protected async submitQuestion(): Promise<void> {
    this.documentForm.markAllAsTouched();

    if (!this.canSubmit) {
      return;
    }

    const request: DocumentAskRequest = {
      DocumentText: this.documentTextControl.value.trim(),
      Question: this.questionControl.value.trim(),
    };

    this.isSubmitting.set(true);
    this.latestResult.set({
      question: request.Question,
      state: 'loading',
    });

    try {
      const response = await firstValueFrom(this.documentQaService.ask(request));

      this.latestResult.update((entry) => this.createSuccessEntry(entry, response));

      this.questionControl.reset('');
      this.questionControl.markAsPristine();
      this.questionControl.markAsUntouched();
    } catch (error) {
      const apiError = this.normalizeApiError(error);

      this.latestResult.update((entry) => {
        if (!entry) {
          return entry;
        }

        return {
          ...entry,
          state: 'error',
          errorTitle: apiError.title,
          errorMessage: apiError.message,
        };
      });
    } finally {
      this.isSubmitting.set(false);
    }
  }

  protected formatUsage(usage?: DocumentAskUsage): string {
    if (!usage) {
      return 'Usage unavailable';
    }

    return `${usage.TotalTokens.toLocaleString()} total tokens (${usage.PromptTokens.toLocaleString()} prompt / ${usage.CompletionTokens.toLocaleString()} completion)`;
  }

  protected formatProcessingTime(processingTimeMs?: number): string {
    return typeof processingTimeMs === 'number'
      ? `${processingTimeMs.toLocaleString()} ms`
      : 'Timing unavailable';
  }

  private createSuccessEntry(
    entry: ResultEntry | null,
    response: DocumentAskResponse
  ): ResultEntry | null {
    if (!entry) {
      return entry;
    }

    return {
      ...entry,
      state: 'success',
      answer: response.Answer,
      metadata: {
        ProcessingTimeMs: response.ProcessingTimeMs,
        Usage: response.Usage,
      },
    };
  }

  private normalizeApiError(error: unknown): DocumentQaApiError {
    if (
      error
      && typeof error === 'object'
      && 'title' in error
      && 'message' in error
    ) {
      return error as DocumentQaApiError;
    }

    return {
      title: 'Unexpected error',
      message: 'The request failed before the response could be rendered.',
    };
  }
}
