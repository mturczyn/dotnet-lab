# What are passkeys?

Passkeys are a replacement for passwords that use cryptographic key pairs. The private key is stored securely on the user's device, such as in a hardware security module, platform authenticator (examples: Windows Hello, Touch ID, Face ID), or a password manager, while the public key is stored by the web app. During authentication, the user proves possession of the private key without it ever leaving their device.

# Prerequisites

.NET SDK (.NET 10 or later)
A modern web browser that supports WebAuthn.
A device with a platform authenticator, such as Windows Hello or Apple secure enclave, or a security key.

# References:

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0