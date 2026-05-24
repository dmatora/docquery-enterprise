export interface DocumentAskRequest {
  DocumentText: string;
  Question: string;
}

export interface DocumentAskUsage {
  PromptTokens: number;
  CompletionTokens: number;
  TotalTokens: number;
}

export interface DocumentAskResponse {
  Answer: string;
  ProcessingTimeMs: number;
  Usage?: DocumentAskUsage;
}
