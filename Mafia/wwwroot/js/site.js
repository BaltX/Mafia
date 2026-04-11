if (window._siteJsLoaded) throw new Error("site.js already loaded");
window._siteJsLoaded = true;

const roleEmojis = {
    "Host": "👤",
    "Civilian": "👨",
    "Mafia": "🖤",
    "Don": "🎰",
    "Maniac": "🔪",
    "Killer": "💀",
    "Commissioner": "🔍",
    "Beauty": "💅",
    "Doctor": "⚕️",
    "Necromancer": "💀"
};

const roleNames = {
    "Unassigned": "Будет назначена при старте",
    "Host": "Ведущий",
    "Civilian": "Мирный житель",
    "Mafia": "Мафия",
    "Don": "Дон мафии",
    "Maniac": "Маньяк",
    "Killer": "Убийца",
    "Commissioner": "Комиссар",
    "Beauty": "Красотка",
    "Doctor": "Доктор",
    "Necromancer": "Некромант"
};

const roleBadgeClasses = {
    "Host": "bg-secondary",
    "Civilian": "bg-success",
    "Mafia": "bg-danger",
    "Don": "bg-danger",
    "Maniac": "bg-dark",
    "Killer": "bg-dark",
    "Commissioner": "bg-primary",
    "Beauty": "bg-warning",
    "Doctor": "bg-info",
    "Necromancer": "bg-purple"
};

const stageNames = {
    "Lobby": "🏠 Ожидание игроков",
    "Discussion": "💬 Обсуждение",
    "DayVoting": "🗳️ Дневное голосование",
    "DiscussionBeforeSecondVote": "💬 Обсуждение",
    "DayVoting2": "🗳️ Второе голосование",
    "NightStart": "🌙 Начало ночи",
    "BeautyTurn": "💅 Ход красотки",
    "DoctorTurn": "⚕️ Ход доктора",
    "CommissionerTurn": "🔍 Ход комиссара",
    "MafiaTurn": "🖤 Ход мафии",
    "KillerTurn": "💀 Ход убийцы",
    "NecromancerTurn": "💀 Ход некроманта",
    "NightResult": "🌙 Результат ночи",
    "GameOver": "🏆 Игра завершена"
};

function getRoleEmoji(role) { return roleEmojis[role] || ""; }
function getRoleName(role) { return roleNames[role] || role; }
function getRoleBadgeClass(role) { return roleBadgeClasses[role] || "bg-secondary"; }
function getStageName(stage) { return stageNames[stage] || stage; }

const votingStages = {
    "DayVoting": "dayVote",
    "DayVoting2": "dayVote",
    "MafiaTurn": "mafiaVote",
    "KillerTurn": "maniacVote",
    "BeautyTurn": "beautyVote",
    "DoctorTurn": "doctorVote",
    "NecromancerTurn": "necromancerVote",
    "CommissionerTurn": "commissionerVote"
};

function escapeHtml(str) {
    if (typeof str !== "string") return "";
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}

function renderPlayersList(players, commissionerChecks, isHost, stage) {
    let html = `<div class="card mb-4"><div class="card-header">👥 Игроки</div><div class="card-body" style="color: #f1f5f9;">`;
    for (const p of players) {
        const roleEmoji = getRoleEmoji(p.role);
        let statusEmoji = "";
        if (!p.isAlive) statusEmoji = "💀";
        else if (p.isZombie) statusEmoji = "🧟";
        else if (p.isBot) statusEmoji = "🤖";

        let checkIcon = "";
        if (commissionerChecks) {
            const playerIdStr = String(p.id);
            if (commissionerChecks[playerIdStr] !== undefined) {
                checkIcon = commissionerChecks[playerIdStr] ? "👎" : "👍";
            }
        }

        const roleDisplay = p.role === "???" ? "Скрыто" : p.role;

        html += `<div class="player-item">`;
        html += `<span class="player-name">${escapeHtml(p.name)}</span> `;
        html += `<span class="badge ${getRoleBadgeClass(p.role)}">${roleEmoji} ${getRoleName(p.role)}</span>${statusEmoji ? ` <span class="status-emoji">${statusEmoji}</span>` : ""}${checkIcon ? ` <span class="status-emoji">${checkIcon}</span>` : ""}`;
        
        if (isHost && stage === "Lobby" && p.role !== "Host") {
            html += renderRoleButtons(players, p.id, p.role);
        }
        
        html += `</div>`;
    }
    html += `</div></div>`;
    return html;
}

function renderRoleButtons(players, playerId, currentRole) {
    const roles = ["Civilian", "Mafia", "Don", "Killer", "Commissioner", "Beauty", "Doctor", "Necromancer"];
    let assignedRoles = roles.filter(r => players.some(p => p.role === r && p.id !== playerId));
    let availableRoles = roles.filter(r => !assignedRoles.includes(r));
    
    let html = `<div class="mt-2 d-flex flex-wrap gap-1 align-items-center">`;
    
    html += `<div class="dropdown">
        <button class="btn btn-outline-secondary btn-sm dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false">Роль</button>
        <ul class="dropdown-menu dropdown-menu-dark">`;
    
    for (const role of availableRoles) {
        html += `<li><form action="/Game/SetPlayerRole" method="post" onsubmit="return handleForm(event)" class="d-inline">
            <input type="hidden" name="code" value="${window._mafiaCode}"/>
            <input type="hidden" name="hostId" value="${window._mafiaHostId}"/>
            <input type="hidden" name="playerId" value="${playerId}"/>
            <input type="hidden" name="role" value="${role}"/>
            <button type="submit" class="dropdown-item btn btn-sm">${getRoleEmoji(role)} ${getRoleName(role)}</button>
        </form></li>`;
    }
    
    html += `</ul></div>`;
    
    if (currentRole !== "Unassigned" && currentRole !== "Civilian") {
        html += `<form action="/Game/ClearPlayerRole" method="post" onsubmit="return handleForm(event)" class="d-inline">
            <input type="hidden" name="code" value="${window._mafiaCode}"/>
            <input type="hidden" name="hostId" value="${window._mafiaHostId}"/>
            <input type="hidden" name="playerId" value="${playerId}"/>
            <button type="submit" class="btn btn-outline-danger btn-sm">Очистить</button>
        </form>`;
    }
    
    html += `</div>`;
    return html;
}

function renderHistory(stageHistory) {
    if (!stageHistory || stageHistory.length === 0) return "";
    let html = `<div class="card mb-4"><div class="card-header">📜 История</div><div class="card-body" style="color: #f1f5f9;">`;
    const reversedHistory = [...stageHistory].reverse();
    for (const entry of reversedHistory) {
        const text = Array.isArray(entry.text) ? entry.text.join("<br/>") : entry.text;
        html += `<div class="mb-3"><strong>Раунд ${entry.round}</strong><br/><span style="color: #94a3b8;">${escapeHtml(entry.stage)}</span><br/>${text}</div>`;
    }
    html += `</div></div>`;
    return html;
}

function getVoteLabel(stage) {
    const labels = {
        "DayVoting": "Голосовать:",
        "DayVoting2": "Голосовать:",
        "MafiaTurn": "Выбрать жертву:",
        "KillerTurn": "Убить:",
        "CommissionerTurn": "Проверить:"
    };
    return labels[stage] || "Выбрать:";
}

function getActionType(stage, voteType) {
    const types = {
        "DoctorTurn": "DoctorAction",
        "BeautyTurn": "BeautyAction",
        "NecromancerTurn": "NecromancerAction"
    };
    if (types[stage]) return types[stage];
    if (stage === "CommissionerTurn") return "CommissionerVote";
    if (voteType === "dayVote") return "DayVote";
    if (voteType === "mafiaVote") return "MafiaVote";
    return "ManiacVote";
}

function getCancelAction(stage, voteType) {
    const cancels = {
        "CommissionerTurn": "CancelCommissionerVote",
        "DoctorTurn": "CancelDoctorVote",
        "BeautyTurn": "CancelBeautyVote",
        "NecromancerTurn": "CancelNecromancerVote"
    };
    if (cancels[stage]) return cancels[stage];
    if (voteType === "dayVote") return "CancelDayVote";
    if (voteType === "mafiaVote") return "CancelMafiaVote";
    return "CancelManiacVote";
}

function renderVotingPanel(code, playerId, data) {
    const voteType = votingStages[data.stage];
    const votingTargetIds = data.votingTargets || [];
    const targetPlayers = data.players.filter(p => votingTargetIds.includes(p.id));
    const currentPlayerVote = data.currentVote;

    if (!voteType || targetPlayers.length === 0) return "";

    const voteLabel = getVoteLabel(data.stage);
    const actionType = getActionType(data.stage, voteType);
    const cancelAction = getCancelAction(data.stage, voteType);

    let html = `<div id="voting-panel"><div class="card mb-4"><div class="card-header">🗳️ ${voteLabel}</div><div class="card-body" style="color: #f1f5f9;">`;

    if (currentPlayerVote) {
        const votedPlayer = data.players.find(p => p.id === currentPlayerVote);
        html += `<div class="mb-3">Выбрано: <strong>${escapeHtml(votedPlayer?.name || 'Неизвестно')}</strong></div>`;

        html += `<form action="/Game/${cancelAction}" method="post" onsubmit="return handleForm(event)" class="d-inline mb-2">`;
        html += `<input type="hidden" name="code" value="${code}"/>`;
        html += `<input type="hidden" name="playerId" value="${playerId}"/>`;
        html += `<button type="submit" class="btn btn-outline-danger btn-sm">Отменить</button></form>`;

        html += `<div class="d-flex flex-wrap gap-2 mt-2">`;
        for (const p of targetPlayers) {
            html += renderVoteButton(code, playerId, actionType, p.id, p.name, currentPlayerVote === p.id);
        }
        html += `</div>`;
    } else {
        html += `<div class="d-flex flex-wrap gap-2 mt-2">`;
        for (const p of targetPlayers) {
            html += renderVoteButton(code, playerId, actionType, p.id, p.name, false);
        }
        html += `</div>`;
    }

    if (data.stage === "CommissionerTurn" && data.commissionerKillTargets?.length > 0 && !data.hasCommissionerCheckPending) {
        const selectedIsKill = data.commissionerIsKill;
        html += `<div class="mt-3 pt-3" style="border-top: 1px solid var(--border);"><strong>Выберите действие:</strong></div>`;
        html += `<div class="d-flex gap-2 mt-2">`;
        
        const isKillSelected = selectedIsKill == true;
        const isCheckSelected = selectedIsKill == false;
        
        html += `<form action="/Game/SetCommissionerIsKill" method="post" onsubmit="return handleForm(event)" class="d-inline">
            <input type="hidden" name="code" value="${code}"/>
            <input type="hidden" name="playerId" value="${playerId}"/>
            <input type="hidden" name="isKill" value="false"/>
            <button type="submit" class="btn ${isCheckSelected ? 'btn-primary' : 'btn-outline-info'} btn-sm">🔍 Проверить</button>
        </form>`;
        
        html += `<form action="/Game/SetCommissionerIsKill" method="post" onsubmit="return handleForm(event)" class="d-inline">
            <input type="hidden" name="code" value="${code}"/>
            <input type="hidden" name="playerId" value="${playerId}"/>
            <input type="hidden" name="isKill" value="true"/>
            <button type="submit" class="btn ${isKillSelected ? 'btn-danger' : 'btn-outline-warning'} btn-sm">🔪 Убить</button>
        </form>`;
        
        html += `</div>`;
    }

    html += `</div></div></div>`;
    return html;
}

function renderNightOverlay() {
    return `<div id="night-overlay">
        <div class="night-curtain">
            <div class="night-logo">
                <div class="logo-icon">🎭</div>
                <div class="logo-title">МАФИЯ</div>
                <div class="logo-subtitle">Ночной ход</div>
                <div class="waiting-dots">
                    <span></span><span></span><span></span>
                </div>
            </div>
        </div>
    </div>`;
}

function renderVoteButton(code, playerId, actionType, targetId, targetName, isSelected) {
    return `<form action="/Game/${actionType}" method="post" onsubmit="return handleForm(event)" class="d-inline">
        <input type="hidden" name="code" value="${code}"/>
        <input type="hidden" name="playerId" value="${playerId}"/>
        <input type="hidden" name="targetId" value="${targetId}"/>
        <button type="submit" class="btn ${isSelected ? 'btn-primary' : 'btn-outline-primary'} btn-sm">${escapeHtml(targetName)}</button>
    </form>`;
}

function initGameApp() {
    const app = document.getElementById("game-app");
    if (!app) return;

    const code = app.dataset.code;
    const playerId = app.dataset.playerId;
    
    const header = document.getElementById("game-header");
    const headerText = document.getElementById("header-text");
    const content = document.getElementById("lobby-content");

    let endsAt = 0;
    let timerTitle = "";
    let scrollPos = 0;
    const timerStages = ["Discussion", "DayVoting", "DayVoting2", "DiscussionBeforeSecondVote", "MafiaTurn", "KillerTurn", "BeautyTurn", "DoctorTurn", "NecromancerTurn"];

    function renderHeader(data) {
        const roleEmoji = getRoleEmoji(data.currentPlayer.role);
        const roleName = getRoleName(data.currentPlayer.role);
        const statusEmoji = data.currentPlayer.isAlive ? "✅" : "💀";
        const stageName = getStageName(data.stage);
        
        let html = `<div class="d-flex flex-wrap align-items-center gap-3 mb-2">`;
        html += `<span>🎭 ${roleEmoji} <strong>${roleName}</strong></span>`;
        html += `<span>${statusEmoji}</span>`;
        html += `<span>📋 ${stageName}</span>`;
        html += `<span>🔢 Раунд: <strong>${data.round}</strong></span>`;
        
        if (data.isHost && data.stage !== "GameOver") {
            if (data.stage === "Lobby") {
                html += `<form action="/Game/StartGame" method="post" onsubmit="return handleForm(event)" class="d-inline">
                    <input type="hidden" name="code" value="${code}"/>
                    <input type="hidden" name="playerId" value="${playerId}"/>
                    <button type="submit" class="btn btn-primary btn-sm">🚀 Старт</button>
                </form>
                <form action="/Game/AddBot" method="post" onsubmit="return handleForm(event)" class="d-inline">
                    <input type="hidden" name="code" value="${code}"/>
                    <input type="hidden" name="playerId" value="${playerId}"/>
                    <button type="submit" class="btn btn-outline-primary btn-sm">🤖 Бот</button>
                </form>`;
            } else {
                html += `<form action="/Game/NextStage" method="post" onsubmit="return handleForm(event)" class="d-inline">
                    <input type="hidden" name="code" value="${code}"/>
                    <input type="hidden" name="playerId" value="${playerId}"/>
                    <button type="submit" class="btn btn-warning btn-sm">⏭️ Далее</button>
                </form>`;
            }
        }
        html += `</div>`;

        const hasTimer = timerStages.includes(data.stage) && data.stageEndsAtUtc;
        if (hasTimer) {
            const titleMap = {
                "Discussion": "💬",
                "DiscussionBeforeSecondVote": "💬",
                "DayVoting": "🗳️",
                "DayVoting2": "🗳️"
            };
            timerTitle = titleMap[data.stage] || "⏱️";
            endsAt = data.stageEndsAtUtc;
        }
        html += `<div class="mt-2">⏱️ Осталось: <span id="header-timer-value" class="${hasTimer ? 'timer-active' : ''} fw-bold">${hasTimer ? '--:--' : '—'}</span></div>`;

        headerText.innerHTML = html;
    }

    function renderTimer() {
        if (!endsAt) return;
        const timerValue = document.getElementById("header-timer-value");
        if (!timerValue) return;

        const now = Date.now();
        const remainingMs = Math.max(0, endsAt - now);
        const totalSec = Math.floor(remainingMs / 1000);
        const min = Math.floor(totalSec / 60);
        const sec = totalSec % 60;
        timerValue.textContent = `${min}:${sec.toString().padStart(2, "0")}`;
    }

    function startTimerInterval() {
        if (window.timerInterval) clearInterval(window.timerInterval);
        window.timerInterval = setInterval(renderTimer, 1000);
    }

    function renderContent(data) {
        let html = "";

        if (data.stage === "GameOver" && data.winnerText) {
            html += `<div class="alert alert-success text-center"><h2 class="h4 mb-0">🏆 Победили: ${escapeHtml(data.winnerText)}</h2></div>`;
        }

        if (data.showNightOverlay) {
            html += renderNightOverlay();
        } else {
            html += renderVotingPanel(code, playerId, data);
        }

        if (!data.showNightOverlay) {
            html += `<div class="row g-4">`;
            html += `<div class="col-lg-6">${renderPlayersList(data.players, data.commissionerChecks, data.isHost, data.stage)}</div>`;

            if (data.stageHistory && data.stageHistory.length > 0) {
                html += `<div class="col-lg-6">${renderHistory(data.stageHistory)}</div>`;
            }

            html += `</div>`;
        }
        
        content.innerHTML = html;
        
        requestAnimationFrame(() => window.scrollTo(0, scrollPos));
    }

    async function refreshLobby() {
        try {
            const response = await fetch(`/Game/LobbyState?code=${encodeURIComponent(code)}&playerId=${encodeURIComponent(playerId)}`, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (response.ok) {
                const data = await response.json();
                
                const stageChanged = window.lastStage !== data.stage;
                window.lastStage = data.stage;
                
                const lastData = window.lastGameData;
                window.lastGameData = data;
                
                const dataChanged = !lastData || JSON.stringify(lastData.players) !== JSON.stringify(data.players) ||
                    lastData.currentVote !== data.currentVote ||
                    lastData.commissionerIsKill !== data.commissionerIsKill ||
                    JSON.stringify(lastData.stageHistory) !== JSON.stringify(data.stageHistory);
                
                if (stageChanged || dataChanged) {
                    renderHeader(data);
                }
                
                if (stageChanged) {
                    renderContent(data);
                } else if (dataChanged) {
                    updatePlayersList(data);
                    updateVotingPanel(data);
                }
                
                if (timerStages.includes(data.stage) && data.stageEndsAtUtc) {
                    renderTimer();
                    startTimerInterval();
                } else {
                    if (window.timerInterval) {
                        clearInterval(window.timerInterval);
                        window.timerInterval = null;
                    }
                }
            }
        } catch (e) {
            console.error(e);
        }
    }
    
    function updatePlayersList(data) {
        const container = document.querySelector("#lobby-content .row.g-4");
        if (!container) return;
        
        const playersCol = container.querySelector(".col-lg-6");
        if (!playersCol) return;
        
        playersCol.innerHTML = renderPlayersList(data.players, data.commissionerChecks, data.isHost, data.stage);
        
        const historyCol = container.querySelectorAll(".col-lg-6")[1];
        if (historyCol && data.stageHistory?.length > 0) {
            historyCol.innerHTML = renderHistory(data.stageHistory);
        }
    }
    
    function updateVotingPanel(data) {
        const existingPanel = document.getElementById("voting-panel");
        const container = document.querySelector("#lobby-content");
        
        const voteType = votingStages[data.stage];
        const votingTargetIds = data.votingTargets || [];
        const targetPlayers = data.players.filter(p => votingTargetIds.includes(p.id));
        
        if (voteType && targetPlayers.length > 0) {
            const html = renderVotingPanel(code, playerId, data);
            if (existingPanel) {
                existingPanel.outerHTML = html;
            } else {
                container.insertAdjacentHTML("afterbegin", html);
            }
        } else if (existingPanel) {
            existingPanel.remove();
        }
    }

    window.handleForm = async function(e) {
        e.preventDefault();
        const form = e.target;
        const formData = new FormData(form);
        
        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData
            });
            if (response.ok) {
                refreshLobby();
            } else {
                const data = await response.json();
                if (data.error) {
                    alert(data.error);
                }
            }
        } catch (err) {
            console.error(err);
        }
        return false;
    };

    window._mafiaCode = code;
    window._mafiaHostId = playerId;
    sessionStorage.setItem("mafiaLobbyCode", code);
    sessionStorage.setItem("mafiaPlayerId", playerId);

    refreshLobby();
    setInterval(refreshLobby, 500);
}

function initHomePage() {
    const code = sessionStorage.getItem("mafiaLobbyCode");
    const playerId = sessionStorage.getItem("mafiaPlayerId");
    if (!code || !playerId) {
        return;
    }

    const block = document.getElementById("returnToLobbyBlock");
    const link = document.getElementById("returnToLobbyLink");
    if (!block || !link) {
        return;
    }

    link.href = `/Game/Lobby?code=${encodeURIComponent(code)}&playerId=${encodeURIComponent(playerId)}`;
    block.classList.remove("d-none");
}

document.addEventListener("DOMContentLoaded", function() {
    initGameApp();
    initHomePage();
});