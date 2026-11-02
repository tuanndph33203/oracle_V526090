import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private authUrl = environment.authencationUrl;
    private authKey = environment.microsoftClientKey;

    redirectMicrosoft() {
        window.location.href = `${this.authUrl}authentication/login-microsoft?clientKey=${this.authKey}`;
    }
}
