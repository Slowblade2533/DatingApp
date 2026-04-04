export interface PaginationHeader {
  currentPage: number;
  itemsPerPage: number;
  totalItems: number;
  totalPages: number;
}

export interface PaginatedResult<T> {
  items: T;
  pagination: PaginationHeader;
}
