import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { environment } from '../environments/environment';
import { DocumentQaApiError, DocumentQaService } from './core/api/document-qa.service';
import { DocumentAskRequest, DocumentAskResponse, DocumentAskUsage } from './core/api/document-qa.models';
import { AccessKeyService } from './core/security/access-key.service';

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
  private readonly accessKeyService = inject(AccessKeyService);

  protected readonly title = 'Docquery Web MVP';
  protected readonly environmentLabel = environment.production ? 'Production API' : 'Local API';
  protected readonly apiBaseUrl = environment.apiBaseUrl;
  protected readonly isSubmitting = signal(false);
  protected readonly latestResult = signal<ResultEntry | null>(null);
  protected readonly hasAccessKey = this.accessKeyService.hasAccessKey;
  protected readonly isEditingAccessKey = signal(!this.accessKeyService.hasAccessKey());
  protected readonly maskedAccessKey = computed(() => this.maskAccessKey(this.accessKeyService.accessKey()));
  protected readonly accessKeyForm = new FormGroup({
    accessKey: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/\S/)],
    }),
  });
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

  protected get accessKeyControl(): FormControl<string> {
    return this.accessKeyForm.controls.accessKey;
  }

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

  protected get canSubmit(): boolean {
    return this.hasAccessKey() && this.documentForm.valid && !this.isSubmitting();
  }

  protected saveAccessKey(): void {
    this.accessKeyForm.markAllAsTouched();

    if (this.accessKeyForm.invalid) {
      return;
    }

    this.accessKeyService.setAccessKey(this.accessKeyControl.value);
    this.resetAccessKeyInput();
    this.isEditingAccessKey.set(false);
  }

  protected startAccessKeyEdit(): void {
    this.resetAccessKeyInput(this.accessKeyService.accessKey());
    this.isEditingAccessKey.set(true);
  }

  protected cancelAccessKeyEdit(): void {
    if (!this.hasAccessKey()) {
      return;
    }

    this.resetAccessKeyInput();
    this.isEditingAccessKey.set(false);
  }

  protected clearAccessKey(): void {
    this.accessKeyService.clearAccessKey();
    this.resetAccessKeyInput();
    this.isEditingAccessKey.set(false);
  }

  protected async submitQuestion(): Promise<void> {
    this.documentForm.markAllAsTouched();

    if (!this.hasAccessKey()) {
      this.accessKeyForm.markAllAsTouched();
      return;
    }

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

  private resetAccessKeyInput(value = ''): void {
    this.accessKeyForm.reset({
      accessKey: value,
    });
  }

  private maskAccessKey(accessKey: string): string {
    if (accessKey.length === 0) {
      return 'Not configured';
    }

    if (accessKey.length <= 8) {
      return 'Stored in browser';
    }

    return `${accessKey.slice(0, 4)}...${accessKey.slice(-4)}`;
  }
}
