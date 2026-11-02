# AN SPA

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Structure

```
src/
├── app/
│   ├── core/                         # Chỉ load 1 lần duy nhất (singleton)
│   │   ├── layout/                   # Layout dùng chung
│   │   │   ├── main-layout/
│   │   │   │   ├── main-layout.ts
│   │   │   │   ├── main-layout.html
│   │   │   │   └── main-layout.scss
│   │   │   ├── header/
│   │   │   └── footer/
│   │   │
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts
│   │   │   └── error.interceptor.ts
│   │   │
│   │   ├── guards/
│   │   │   └── auth.guard.ts
│   │   │
│   │   ├── services/                # Service global, Singleton
│   │   │   ├── http.service.ts
│   │   │   ├── storage.service.ts
│   │   │   └── seo.service.ts
│   │   │
│   │   ├── constants/
│   │   │   ├── api.constant.ts
│   │   │   ├── app.constant.ts
│   │   │   └── environment.constant.ts
│   │   │
│   │   ├── models/
│   │   │   ├── user.model.ts
│   │   │   ├── api-response.model.ts
│   │   │   └── movie.model.ts
│   │   │
│   │   └── utils/
│   │       ├── date.util.ts
│   │       ├── slug.util.ts
│   │       └── pagination.util.ts
│   │
│   │
│   ├── feature/                     # Feature-based (mỗi module = 1 domain)
│   │   ├── home/
│   │   │   ├── pages/
│   │   │   │   └── home/
│   │   │   │       ├── home.ts
│   │   │   │       ├── home.html
│   │   │   │       └── home.scss
│   │   │   │
│   │   │   ├── components/
│   │   │   │   ├── banner/
│   │   │   │   ├── movie-card/
│   │   │   │   └── search-box/
│   │   │   │
│   │   │   ├── services/
│   │   │   │   └── home.service.ts
│   │   │   │
│   │   │   ├── models/
│   │   │   │   └── home.model.ts
│   │   │   │
│   │   │   └── home.routes.ts
│   │   │
│   │   ├── movie-detail/
│   │   │   ├── pages/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   └── movie-detail.routes.ts
│   │   │
│   │   ├── watch/
│   │   │   ├── pages/
│   │   │   ├── components/
│   │   │   └── watch.routes.ts
│   │   │
│   │   ├── category/
│   │   │   ├── pages/
│   │   │   └── category.routes.ts
│   │   │
│   │   └── profile/
│   │       ├── pages/
│   │       └── profile.routes.ts
│   │
│   │
│   ├── share/                       # Shared for entire App
│   │   ├── ui/
│   │   │   ├── button/
│   │   │   ├── card/
│   │   │   └── rating/
│   │   │
│   │   ├── pipes/
│   │   │   ├── safe-html.pipe.ts
│   │   │   └── truncate.pipe.ts
│   │   │
│   │   ├── directives/
│   │   │   └── lazy-load.directive.ts
│   │   │
│   │   └── services/
│   │       └── toast.service.ts
│   │
│   │
│   ├── app.routes.ts
│   ├── app.config.ts
│   ├── app.component.ts
│   ├── app.component.html
│   └── app.scss
│
├── assets/
│   ├── images/
│   ├── icons/
│   └── styles/
│       ├── _variables.scss
│       └── _mixins.scss
│
└── environments/
    ├── environment.ts
    ├── environment.dev.ts
    └── environment.prod.ts


```
