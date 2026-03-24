import { Routes } from '@angular/router';
import { Home } from '../features/home/home';
import { authGuard } from '../core/guards/auth-guard';

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
    path: '**',
    redirectTo: '',
  },
];
