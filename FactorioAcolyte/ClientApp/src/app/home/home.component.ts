import { Component, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { error } from 'util';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  public userName = '';
  public otp = '';
  public status = '';
  token = '';
  expiry = new Date();

  isWorking = false;
  isSummoning = false;
  isChecking = false;
  isCheckSuccess = false;
  isCheckFail = false;
  isDestroying = false;

  constructor(private http: HttpClient) { }

  reset() {
    this.isWorking = false;
    this.isSummoning = false;
    this.isChecking = false;
    this.isCheckSuccess = false;
    this.isCheckFail = false;
    this.isDestroying = false;
    this.status = '';
  }

  summon() {
    this.reset();

    console.log('SUMMON');

    if (typeof this.token != 'undefined' && this.token) {
      this.isSummoning = true;
      this.isWorking = true;
      this.status = 'Summoning has begun...';

      this.http.get<CheckResponse>('http://roman015.com/Factorio/Start', this.getAuthorizedHeader())
        .subscribe(
          result => {
            this.summonComplete(result.result);
          },
          error => {
            console.error(error);
            this.isChecking = false;
            this.isWorking = false;
            this.isSummoning = false;
          });
    } else {
      this.isChecking = false;
      this.isWorking = false;
      this.isSummoning = false;
      this.status = 'Authorization invalid';
    }
  }

  summonComplete(message: string) {
    this.status = message;
    this.isWorking = false;
    this.isSummoning = false;
  }

  check() {
    this.reset();

    console.log('CHECK');

    if (typeof this.token != 'undefined' && this.token) {
      this.isChecking = true;
      this.isWorking = true;
      this.status = "All I see is blackness. Oh, my hood's down, one moment...";

      this.http.get<CheckResponse>('http://roman015.com/Factorio/Check', this.getAuthorizedHeader())
        .subscribe(
          result => {
            this.checkComplete(result.result);
          },
          error => {
            console.error(error);
            this.isChecking = false;
            this.isWorking = false;
          });
    } else {
      this.isDestroying = false;
      this.isWorking = false;
      this.status = 'Authorization invalid';
    }
  }

  checkComplete(message: string) {
    this.status = message;

    this.isChecking = false;
    this.isWorking = false;

    if (message == 'success') {
      this.isCheckSuccess = true;
    } else {
      this.isCheckFail = true;
    }
  }

  destroy() {
    console.log('DESTROY');

    if (typeof this.token != 'undefined' && this.token) {
      this.isDestroying = true;
      this.isWorking = true;
      this.status = 'Let life cease!';

      this.http.get<CheckResponse>('http://roman015.com/Factorio/Stop', this.getAuthorizedHeader())
        .subscribe(
          result => {
            this.destroyComplete(result.result);
          },
          error => {
            console.error(error);
          });
    } else {
      this.isDestroying = false;
      this.isWorking = false;
      this.status = 'Authorization invalid';
    }
  }

  destroyComplete(message: string) {
    this.status = message;
    this.isWorking = false;
    this.isDestroying = false;
  }

  login() {
    if (typeof this.token == 'undefined' || !(this.token) || this.expiry || new Date() < this.expiry) {
      this.status = 'Is That you Master?'
      this.isWorking = true;

      var request = new LoginRequest();
      request.Email = this.userName;
      request.Otp = this.otp;

      this.http.post<LoginResponse>('http://roman015.com/Authenticate/Login',
        request)
        .subscribe(
          result => {
            this.status = 'Greetings, Master';
            this.token = result.token;
            this.isWorking = false;
          },
          error => {
            this.status = 'This is not my master! You are an imposter!'
            this.isWorking = false;
          });
    }
  }

  getAuthorizedHeader() {
    var httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + this.token
      })
    };

    return httpOptions;
  }
}

class LoginRequest {
  Email: string;
  Otp: string;
}

interface LoginResponse {
  token: string;
}

interface CheckResponse {
  result: string
}
