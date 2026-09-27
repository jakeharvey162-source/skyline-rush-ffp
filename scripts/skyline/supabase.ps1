$ErrorActionPreference = "Stop"

# Skyline Rush cloud lobby bridge.
# The publishable key is intentionally safe for client-side use.
# Never place a Supabase secret/service-role key in this repository.
$script:SkylineSupabaseUrl = "https://ftsomveafuskrutqzsvs.supabase.co"
$script:SkylineSupabasePublishableKey = "sb_publishable_x3SYM26ShPtf_JIYdvOkEg_kLXb2kIz"

function Invoke-SkylineSupabaseRpc {
    param(
        [Parameter(Mandatory = $true)][string]$Rpc,
        [Parameter(Mandatory = $true)][string]$AccessToken,
        [Parameter(Mandatory = $true)][hashtable]$Body
    )

    if ([string]::IsNullOrWhiteSpace($AccessToken)) {
        throw "A signed-in Supabase access token is required."
    }

    $headers = @{
        apikey = $script:SkylineSupabasePublishableKey
        Authorization = "Bearer $AccessToken"
    }

    return Invoke-RestMethod `
        -Method Post `
        -Uri "$script:SkylineSupabaseUrl/rest/v1/rpc/$Rpc" `
        -Headers $headers `
        -ContentType "application/json" `
        -Body ($Body | ConvertTo-Json -Compress)
}

function New-SkylineCloudRoom {
    param(
        [Parameter(Mandatory = $true)][string]$AccessToken,
        [Parameter(Mandatory = $true)]
        [ValidateSet("duel","2v2","4v4","coop2","coop4")]
        [string]$Mode,
        [Parameter(Mandatory = $true)]
        [ValidateSet("heights","lagoon","rift")]
        [string]$District,
        [Parameter(Mandatory = $true)][string]$HostAddress,
        [int]$ServerPort = 29801,
        [string]$ServerPassword
    )

    return Invoke-SkylineSupabaseRpc -Rpc "skyline_create_room" -AccessToken $AccessToken -Body @{
        p_mode = $Mode
        p_district = $District
        p_host_address = $HostAddress
        p_server_port = $ServerPort
        p_server_password = $ServerPassword
    }
}

function Join-SkylineCloudRoom {
    param(
        [Parameter(Mandatory = $true)][string]$AccessToken,
        [Parameter(Mandatory = $true)]
        [ValidatePattern("^[A-Fa-f0-9]{6}$")]
        [string]$RoomCode
    )

    return Invoke-SkylineSupabaseRpc -Rpc "skyline_join_room" -AccessToken $AccessToken -Body @{
        p_room_code = $RoomCode.ToUpperInvariant()
    }
}

# Member-only lobby state; no service-role key is needed.
function Get-SkylineLobby {
    param([Parameter(Mandatory=$true)][string]$AccessToken,
          [Parameter(Mandatory=$true)][guid]$RoomId)
    Invoke-SkylineSupabaseRpc -Rpc "skyline_lobby_action" -AccessToken $AccessToken -Body @{p_room_id=$RoomId.ToString();p_action="view"}
}
function Set-SkylineReady {
    param([Parameter(Mandatory=$true)][string]$AccessToken,
          [Parameter(Mandatory=$true)][guid]$RoomId,
          [Parameter(Mandatory=$true)][bool]$Ready)
    Invoke-SkylineSupabaseRpc -Rpc "skyline_lobby_action" -AccessToken $AccessToken -Body @{p_room_id=$RoomId.ToString();p_action="ready";p_ready=$Ready}
}
function Exit-SkylineLobby {
    param([Parameter(Mandatory=$true)][string]$AccessToken,
          [Parameter(Mandatory=$true)][guid]$RoomId)
    Invoke-SkylineSupabaseRpc -Rpc "skyline_lobby_action" -AccessToken $AccessToken -Body @{p_room_id=$RoomId.ToString();p_action="leave"}
}
