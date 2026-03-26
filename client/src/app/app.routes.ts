import { Routes } from '@angular/router';
import { Home } from '../features/home/home';
import { authGuard } from '../core/guards/auth-guard';
import { TestErrors } from '../features/test-errors/test-errors';
import { NotFound } from '../shared/errors/not-found/not-found';
import { ServerError } from '../shared/errors/server-error/server-error';

export const routes: Routes = [
  {
    path: '',
    component: Home,
  },
  {
    path: '',
    runGuardsAndResolvers: 'always',
    canActivate: [authGuard],
    children: [
      {
        path: 'members',
        loadComponent: () =>
          import('../features/members/member-list/member-list').then((m) => m.MemberList),
        title: 'Members',
      },
      {
        path: 'members/:id',
        loadComponent: () =>
          import('../features/members/member-detailed/member-detailed').then(
            (m) => m.MemberDetailed,
          ),
        title: 'Member Profile',
      },
      {
        path: 'lists',
        loadComponent: () => import('../features/lists/lists').then((m) => m.Lists),
        title: 'Lists',
      },
      {
        path: 'messages',
        loadComponent: () => import('../features/messages/messages').then((m) => m.Messages),
        title: 'Messages',
      },
    ],
  },
  {
    path: 'errors',
    component: TestErrors,
  },
  {
    path: 'server-error',
    component: ServerError,
  },
  {
    path: '**',
    component: NotFound,
  },
];
