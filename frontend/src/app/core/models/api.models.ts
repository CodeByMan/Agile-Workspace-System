export interface ApiResponse<T> {
  Success: boolean;
  Message: string;
  Data: T;
  Errors?: Record<string, string[]>;
}

export interface PagedResult<T> {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  items: T[];
}
