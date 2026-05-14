import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { EditableMember, Member, Photo } from '../../types/member';
import { PaginatedResult, PaginationHeader } from '../../types/pagination';

@Injectable({
  providedIn: 'root',
})
export class MemberService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  editMode = signal(false);
  member = signal<Member | null>(null);

  getMembers(pageNumber: number, pageSize: number) {
    // ใส่ pageNumber และ pageSize เป็น Query String (?pageNumber=1&pageSize=10)
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http
      .get<Member[]>(this.baseUrl + 'members', {
        observe: 'response', // 👈 ขอดู Response เต็ม ไม่ใช่แค่ body เพื่อจะได้อ่าน Header ได้
        params,
      })
      .pipe(
        map((response) => {
          // ดึง Pagination จาก Header
          const paginationHeader = response.headers.get('Pagination');
          let paginationData: PaginationHeader = {
            currentPage: 1,
            itemsPerPage: pageSize,
            totalItems: 0,
            totalPages: 0,
          };
          if (paginationHeader) {
            try {
              paginationData = JSON.parse(paginationHeader);
            } catch (error) {
              console.error('Failed to parse pagination header', error);
            }
          }
          const result: PaginatedResult<Member[]> = {
            items: response.body ?? [],
            pagination: paginationData,
          };

          return result;
        }),
      );
  }

  getMember(id: string) {
    return this.http.get<Member>(this.baseUrl + 'members/' + id).pipe(
      tap((member) => {
        this.member.set(member);
      }),
    );
  }

  getMemberPhotos(id: string) {
    return this.http.get<Photo[]>(this.baseUrl + 'members/' + id + '/photos');
  }

  updateMember(member: EditableMember) {
    return this.http.put(this.baseUrl + 'members', member);
  }
}
