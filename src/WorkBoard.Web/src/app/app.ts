import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * The bootstrap component.
 *
 * Deliberately empty: the router decides between the auth screens and the application shell, and
 * putting chrome here would render it on the login page too.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
