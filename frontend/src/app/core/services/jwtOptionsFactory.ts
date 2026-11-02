export function jwtOptionsFactory() {
    return {
        tokenGetter: () => localStorage.getItem('authToken'),
        allowedDomains: [],
        disallowedRoutes: []
    };
}
