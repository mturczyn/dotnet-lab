const apiBaseUrl = 'http://localhost:5025'

async function createCredential(headers: Headers, signal: AbortSignal) {
    // Request creation options from the server
    const optionsResponse = await fetch(
        `${apiBaseUrl}/accountv2/PasskeyCreationOptions`,
        {
            method: 'POST',
            headers,
            signal,
        }
    )
    const optionsJson = await optionsResponse.json()
    const options =
        PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson)
    return await navigator.credentials.create({ publicKey: options, signal })
}

async function createCredentialJson(headers: Headers, signal: AbortSignal) {
    const credential = await createCredential(headers, signal)
    return JSON.stringify(credential)
}

async function requestCredential(
    email: string,
    mediation: CredentialMediationRequirement,
    headers: Headers,
    signal: AbortSignal
) {
    // Step 2: Request authentication options from the server
    const optionsResponse = await fetch(
        `${apiBaseUrl}/accountv2/PasskeyRequestOptions?username=${encodeURIComponent(email)}`,
        {
            method: 'POST',
            headers,
            signal,
        }
    )
    const optionsJson = await optionsResponse.json()
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson)
    return await navigator.credentials.get({
        publicKey: options,
        mediation,
        signal,
    })
}
