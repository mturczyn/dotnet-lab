import { Link } from '@tanstack/react-router';

export default function HomePage() {
  return (
    <>
      <h1>Passkeys with ASP.NET Core Identity</h1>
      <p>
        Demo companion to the blog post "Passkeys in ASP.NET Core 10:
        Passwordless Authentication with Identity".
      </p>
      <nav>
        <Link to="/register">Register</Link>
        <Link to="/login">Sign in</Link>
      </nav>
    </>
  );
}
