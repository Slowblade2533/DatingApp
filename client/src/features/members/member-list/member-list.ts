import { Component, inject, signal } from '@angular/core';
import { MemberService } from '../../../core/services/member.service';
import { combineLatest, switchMap } from 'rxjs';
import { MemberCard } from '../member-card/member-card';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-member-list',
  imports: [MemberCard],
  templateUrl: './member-list.html',
  styleUrl: './member-list.css',
})
export class MemberList {
  private memberService = inject(MemberService);

  pageNumber = signal(1);
  pageSize = signal(10);

  protected paginatedResult = toSignal(
    combineLatest([toObservable(this.pageNumber), toObservable(this.pageSize)]).pipe(
      switchMap(([page, size]) => this.memberService.getMembers(page, size)),
    ),
    { initialValue: null },
  );

  onPageChange(newPage: number) {
    this.pageNumber.set(newPage);
  }
}
