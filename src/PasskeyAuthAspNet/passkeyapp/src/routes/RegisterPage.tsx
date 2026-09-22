import { useState, type FormEvent } from 'react'
import { Link } from '@tanstack/react-router'
import { apiUrl } from '../lib/api'
import { registerPasskey } from '../lib/passkeys'

export default function RegisterPage() {
    const [status, setStatus] = useState('')
    const [regUsername, setRegUsername] = useState('')
    const [regPassword, setRegPassword] = useState('')
    const [plUsername, setPlUsername] = useState('')
    const [canAddPasskey, setCanAddPasskey] = useState(false)

    const handlePasswordRegister = async (e: FormEvent) => {
        e.preventDefault()
        const response = await fetch(apiUrl('/accountv2/register'), {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: regUsername,
                password: regPassword,
            }),
        })

        if (response.ok) {
            setStatus(
                `Account "${regUsername}" created and signed in. You can now add a passkey.`
            )
            setCanAddPasskey(true)
        } else {
            setStatus(`Registration failed: ${await response.text()}`)
        }
    }

    const handleAddPasskey = async () => {
        try {
            await registerPasskey()
            setStatus('Passkey registered successfully.')
        } catch (err) {
            setStatus(`Passkey registration failed: ${(err as Error).message}`)
        }
    }

    const handlePasswordlessRegister = async (e: FormEvent) => {
        e.preventDefault()

        const createResponse = await fetch(
            apiUrl('/accountv2/register-passwordless'),
            {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username: plUsername }),
            }
        )

        if (!createResponse.ok) {
            setStatus(`Account creation failed: ${await createResponse.text()}`)
            return
        }

        try {
            await registerPasskey()
            setStatus(
                `Passwordless account "${plUsername}" created with a passkey. You can sign in with it now.`
            )
        } catch (err) {
            setStatus(
                `Account created, but the passkey ceremony failed: ${(err as Error).message}`
            )
        }
    }

    return (
        <>
            <h1>Create an account</h1>
            <nav>
                <Link to="/">Home</Link>
                <Link to="/login">Sign in</Link>
            </nav>

            <form onSubmit={handlePasswordRegister}>
                <h2>With a password</h2>
                <input
                    type="text"
                    placeholder="Username"
                    autoComplete="username"
                    required
                    value={regUsername}
                    onChange={(e) => setRegUsername(e.target.value)}
                />
                <input
                    type="password"
                    placeholder="Password"
                    autoComplete="new-password"
                    required
                    value={regPassword}
                    onChange={(e) => setRegPassword(e.target.value)}
                />
                <button type="submit">Register</button>
                <button
                    type="button"
                    disabled={!canAddPasskey}
                    onClick={handleAddPasskey}
                >
                    Add a passkey to this account
                </button>
            </form>

            <form onSubmit={handlePasswordlessRegister}>
                <h2>Passwordless (passkey only)</h2>
                <input
                    type="text"
                    placeholder="Username"
                    autoComplete="username"
                    required
                    value={plUsername}
                    onChange={(e) => setPlUsername(e.target.value)}
                />
                <button type="submit">Create account with a passkey</button>
            </form>

            <div id="status">{status}</div>
        </>
    )
}
