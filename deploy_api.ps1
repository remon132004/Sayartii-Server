# Deploy Sayartii API to Hugging Face Spaces
param(
    [string]$Token = ""
)

if ($Token -eq "") {
    Write-Host "Enter your Hugging Face Access Token:" -ForegroundColor Yellow
    $secToken = Read-Host -AsSecureString
    $Token = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secToken)
    )
}

$hfUser = "remon132004"
$repoPath = "D:\flutter\Sayartii-Server"

$apiUrl = "https://" + $hfUser + ":" + $Token + "@huggingface.co/spaces/" + $hfUser + "/sayartii-api"

Write-Host ""
Write-Host "[*] Setting remote URL with token..." -ForegroundColor Cyan
git -C $repoPath remote set-url hf-api $apiUrl

Write-Host "[*] Pushing .NET API backend to HF Space..." -ForegroundColor Green
git -C $repoPath subtree push --prefix=backend hf-api main

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Push DONE! API will rebuild on HF in ~2-3 minutes." -ForegroundColor Green
} else {
    Write-Host "[ERR] Push FAILED. Check token and try again." -ForegroundColor Red
}

# Remove token from remote URL for security
git -C $repoPath remote set-url hf-api "https://huggingface.co/spaces/remon132004/sayartii-api"
Write-Host "[*] Token removed from remote for security." -ForegroundColor Gray
