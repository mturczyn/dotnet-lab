import { apiUrl } from './api'

export type PasskeyMediation =
    | 'conditional'
    | 'optional'
    | 'required'
    | 'silent'

async function createPasskey(): Promise<PublicKeyCredential> {
    const response = await fetch(apiUrl('/accountv2/passkey-options'), {
        method: 'POST',
        credentials: 'include',
    })
    if (!response.ok) {
        throw new Error(`Could not fetch creation options (${response.status})`)
    }
    const optionsJson =
        (await response.json()) as PublicKeyCredentialCreationOptionsJSON
    const options =
        PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson)

    try {
        // Prompts Windows Hello / Touch ID / a security key - entirely browser-handled.
        const credential = await navigator.credentials.create({
            publicKey: options,
        })
        if (!credential) {
            throw new Error('No credential was returned by the authenticator.')
        }
        return credential as PublicKeyCredential
    } catch (err) {
        // NotAllowedError: user cancelled the OS/biometric prompt, or it timed out
        // InvalidStateError: an authenticator for this account already has a credential registered
        // SecurityError: origin doesn't match the configured ServerDomain / RP ID
        const error = err as Error
        throw new Error(
            `Passkey creation failed at CreatePasskey. Inner Error: ${error.name} - ${error.message}`
        )
    }
}

export async function registerPasskey(): Promise<void> {
    const credential = await createPasskey()
    const credentialJson = JSON.stringify(credential.toJSON())

    const response = await fetch(apiUrl('/accountv2/passkey-register'), {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: credentialJson,
    })

    if (!response.ok) {
        throw new Error(
            `Server rejected the passkey (${await response.text()})`
        )
    }
}

export async function signInWithPasskey(
    username?: string,
    mediation: PasskeyMediation = 'optional'
): Promise<void> {
    const query = username ? `?username=${encodeURIComponent(username)}` : ''
    const response = await fetch(
        apiUrl(`/accountv2/passkey-request-options${query}`),
        {
            method: 'POST',
            credentials: 'include',
        }
    )
    if (!response.ok) {
        throw new Error(`Could not fetch request options (${response.status})`)
    }
    const optionsJson =
        (await response.json()) as PublicKeyCredentialRequestOptionsJSON
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson)

    let credential: Credential | null
    try {
        // 'conditional' shows saved passkeys as autofill suggestions instead of popping
        // a dialog immediately - the caller controls this per the article's guidance.
        credential = await navigator.credentials.get({
            publicKey: options,
            mediation,
        })
    } catch (err) {
        // NotAllowedError: user dismissed the prompt, or the request timed out
        const error = err as Error
        console.error('Passkey sign-in failed:', error.name, error.message)
        throw error
    }

    if (!credential) {
        throw new Error('No credential was returned.')
    }

    const credentialJson = JSON.stringify(
        (credential as PublicKeyCredential).toJSON()
    )
    const signInResponse = await fetch(apiUrl('/accountv2/passkey-signin'), {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: credentialJson,
    })

    if (!signInResponse.ok) {
        throw new Error(`Sign-in rejected (${signInResponse.status})`)
    }
}
