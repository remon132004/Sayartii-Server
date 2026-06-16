# =====================================================
# Sayartii - Push to Hugging Face Spaces
# =====================================================
# Usage: .\push_to_hf.ps1
# You will be asked for your HF username + token
# =====================================================

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Sayartii -> Hugging Face Deploy" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Ask for HF token once
$hfUser = "remon132004"
Write-Host "Enter your Hugging Face Access Token (from https://huggingface.co/settings/tokens):" -ForegroundColor Yellow
$hfToken = Read-Host -AsSecureString
$plainToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($hfToken))

# Build remote URLs with auth
$apiUrl = "https://$($hfUser):$($plainToken)@huggingface.co/spaces/$($hfUser)/sayartii-api"
$aiUrl  = "https://$($hfUser):$($plainToken)@huggingface.co/spaces/$($hfUser)/sayartii-ai"

# Update remotes with token embedded
git remote set-url hf-api $apiUrl
git remote set-url hf-ai  $aiUrl

Write-Host ""
Write-Host "[1/2] Pushing .NET API Backend to HF Space..." -ForegroundColor Green
git subtree push --prefix=backend hf-api main

if ($LASTEXITCODE -eq 0) {
    Write-Host "[1/2] API push DONE!" -ForegroundColor Green
} else {
    Write-Host "[1/2] API push FAILED. Check your token or Space name." -ForegroundColor Red
}

Write-Host ""
Write-Host "[2/2] Pushing Flask AI Service to HF Space..." -ForegroundColor Green
git subtree push --prefix=flask_backend hf-ai main

if ($LASTEXITCODE -eq 0) {
    Write-Host "[2/2] AI push DONE!" -ForegroundColor Green
} else {
    Write-Host "[2/2] AI push FAILED. Check your token or Space name." -ForegroundColor Red
}

# Remove token from remotes for security
git remote set-url hf-api "https://huggingface.co/spaces/$($hfUser)/sayartii-api"
git remote set-url hf-ai  "https://huggingface.co/spaces/$($hfUser)/sayartii-ai"

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Deploy Complete!" -ForegroundColor Cyan
Write-Host "  API:  https://$($hfUser)-sayartii-api.hf.space" -ForegroundColor White
Write-Host "  AI:   https://$($hfUser)-sayartii-ai.hf.space" -ForegroundColor White
Write-Host "=====================================" -ForegroundColor Cyan
