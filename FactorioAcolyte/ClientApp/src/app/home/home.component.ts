import { Component } from '@angular/core';

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
  isDestroying = false;

  summon() {
    console.log('SUMMON');
    this.isSummoning = true;
    this.isWorking = true;
    this.status = 'Summoning has begun...';
  }

  summonComplete(isSuccess: boolean, message: string) {
    if (isSuccess == true) {
      this.status = 'Summoning is Complete!';
    } else {
      this.status = message;
    }

    this.isWorking = false;
    this.isSummoning = false;
  }

  check() {
    console.log('CHECK');
    this.isChecking = true;
    this.isWorking = true;
    this.status = 'The damned stand ready...';
    this.status = "All I see is blackness. Oh, my hood's down.";
  }

  checkComplete(message: string) {
    this.status = message;

    this.isChecking = false;
    this.isWorking = false;
  }

  destroy() {
    console.log('DESTROY');
    this.isDestroying = true;
    this.isWorking = true;
    this.status = 'Let life cease!';
  }

  destroyComplete(isSuccess: boolean, message: string) {
    if (isSuccess == true) {
      this.status = 'It is done!';
    } else {
      this.status = message;
    }

    this.isWorking = false;
    this.isDestroying = false;
  }
}
