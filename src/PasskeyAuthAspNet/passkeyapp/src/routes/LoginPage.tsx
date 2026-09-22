import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from '@tanstack/react-router'
import { apiUrl } from '../lib/api'
import { signInWithPasskey } from '../lib/passkeys'

export default function LoginPage() {
    const [status, setStatus] = useState('')
    const [username, setUsername] = useState('')
    const [pwUsername, setPwUsername] = useState('')
    const [pwPassword, setPwPassword] = useState('')
    const conditionalAttempted = useRef(false)

    // Conditional UI: offers saved passkeys as native autofill suggestions on the
    // username field as soon as the page loads, before the user types anything.
    // Requires autoComplete="username webauthn" on the input below.
    useEffect(() => {
        if (conditionalAttempted.current) return
        conditionalAttempted.current = true

        if (!window.PublicKeyCredential?.isConditionalMediationAvailable) return

        PublicKeyCredential.isConditionalMediationAvailable().then(
            async (available) => {
                if (!available) return
                try {
                    await signInWithPasskey(undefined, 'conditional')
                    setStatus('Signed in via conditional UI.')
                } catch {
                    // User hasn't picked a suggestion yet - this is expected while the page is idle.
                }
            }
        )
    }, [])

    const handlePasskeySubmit = async (e: FormEvent) => {
        e.preventDefault()
        try {
            await signInWithPasskey(username || undefined)
            setStatus('Signed in with a passkey.')
        } catch (err) {
            setStatus(`Passkey sign-in failed: ${(err as Error).message}`)
        }
    }

    const handlePasswordSubmit = async (e: FormEvent) => {
        e.preventDefault()
        const response = await fetch(apiUrl('/accountv2/login'), {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: pwUsername,
                password: pwPassword,
            }),
        })

        setStatus(
            response.ok ? 'Signed in.' : `Sign-in failed (${response.status}).`
        )
    }

    return (
        <>
            <h1>Sign in</h1>
            <nav>
                <Link to="/">Home</Link>
                <Link to="/register">Register</Link>
            </nav>

            <form onSubmit={handlePasskeySubmit}>
                <h2>With a passkey</h2>
                <input
                    type="text"
                    placeholder="Username (optional)"
                    autoComplete="username webauthn"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                />
                <button type="submit">Sign in with a passkey</button>
            </form>

            <form onSubmit={handlePasswordSubmit}>
                <h2>With a password</h2>
                <input
                    type="text"
                    placeholder="Username"
                    autoComplete="username"
                    required
                    value={pwUsername}
                    onChange={(e) => setPwUsername(e.target.value)}
                />
                <input
                    type="password"
                    placeholder="Password"
                    autoComplete="current-password"
                    required
                    value={pwPassword}
                    onChange={(e) => setPwPassword(e.target.value)}
                />
                <button type="submit">Sign in</button>
            </form>

            <div id="status">{status}</div>
        </>
    )
}
