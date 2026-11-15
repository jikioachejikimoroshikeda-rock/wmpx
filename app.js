const tracks = [
  { title: "A Brighter Day", artist: "Soundscape", duration: "4:05", seconds: 245 },
  { title: "Lost in the Light", artist: "Soundscape", duration: "3:28", seconds: 208 },
  { title: "Better Together", artist: "Soundscape", duration: "4:12", seconds: 252 },
  { title: "Skyline", artist: "Soundscape", duration: "3:56", seconds: 236 },
  { title: "Moments", artist: "Soundscape", duration: "4:20", seconds: 260 },
  { title: "Into the Blue", artist: "Soundscape", duration: "3:48", seconds: 228 },
  { title: "Home Again", artist: "Soundscape", duration: "5:01", seconds: 301 },
  { title: "A Higher Place", artist: "Soundscape", duration: "4:33", seconds: 273 }
];
const upcoming = [
  { title: "Ocean Mind", artist: "Luna Fields", duration: "3:17" },
  { title: "Electric Hearts", artist: "The Motion", duration: "3:45" },
  { title: "New Horizons", artist: "Skyward", duration: "4:08" }
];
let currentTrack = 0;
let isPlaying = true;
let elapsed = 102;
let activeTab = "Lista";
const $ = (selector) => document.querySelector(selector);
const trackList = $("#track-list");
const progress = $("#progress");

function renderTracks(filter = "") {
  const query = filter.trim().toLocaleLowerCase("es");
  trackList.innerHTML = "";
  tracks.forEach((track, index) => {
    if (query && !`${track.title} ${track.artist}`.toLocaleLowerCase("es").includes(query)) return;
    const row = document.createElement("li");
    row.className = `track-row${index === currentTrack ? " active" : ""}`;
    row.innerHTML = `<span class="track-number">${index === currentTrack ? "▮" : index + 1}</span><span class="track-name"></span><time class="track-duration">${track.duration}</time>`;
    row.querySelector(".track-name").textContent = track.title;
    row.addEventListener("click", () => selectTrack(index));
    trackList.append(row);
  });
}
function renderUpcoming() {
  $("#up-next-list").innerHTML = upcoming.map((track) => `<div class="upcoming"><span class="upcoming-cover"></span><span class="upcoming-copy"><strong>${track.title}</strong><small>${track.artist}</small></span><time>${track.duration}</time></div>`).join("");
}
function selectTrack(index) {
  currentTrack = (index + tracks.length) % tracks.length;
  elapsed = 0;
  isPlaying = true;
  updatePlayer();
  renderTracks($("#search").value);
}
function formatTime(seconds) {
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;
}
function updatePlayer() {
  const track = tracks[currentTrack];
  $("#track-title").textContent = track.title;
  $("#track-artist").textContent = track.artist;
  $("#duration").textContent = track.duration;
  progress.max = track.seconds;
  progress.value = elapsed;
  progress.style.setProperty("--fill", `${(elapsed / track.seconds) * 100}%`);
  $("#elapsed").textContent = formatTime(elapsed);
  $("#play").textContent = isPlaying ? "Ⅱ" : "▶";
  $("#play").setAttribute("aria-label", isPlaying ? "Pausar" : "Reproducir");
  if (activeTab === "Lista") renderTracks($("#search").value);
}
function setTab(tab) {
  activeTab = tab;
  document.querySelectorAll(".tab").forEach((button) => button.classList.toggle("active", button.dataset.tab === tab));
  const panel = $("#panel-content");
  if (tab === "Lista") {
    panel.innerHTML = `<div class="queue-heading"><h2>A Higher Place</h2><p>Soundscape <span>•</span> 2024</p></div><ol class="track-list" id="track-list"></ol><div class="up-next"><button class="up-next-title" id="up-next"><span>Siguiente en la reproducción</span><span>⌃</span></button><div id="up-next-list"></div></div>`;
    renderTracks($("#search").value);
    renderUpcoming();
    $("#up-next").addEventListener("click", () => $(".up-next").classList.toggle("collapsed"));
  } else if (tab === "Letras") {
    panel.innerHTML = `<div class="lyrics-view"><h2>${tracks[currentTrack].title}</h2><p>Encuentra tu camino<br>hacia un nuevo amanecer<br><br>Deja que la luz te guíe<br>y vuelve a comenzar</p><small>Las letras son ilustrativas</small></div>`;
  } else {
    panel.innerHTML = `<div class="info-view"><div class="info-art">${$(".album-art").innerHTML}</div><h2>A Higher Place</h2><p>Soundscape<br>Álbum · 2024<br>8 canciones · 33 min</p></div>`;
  }
}

$("#search").addEventListener("input", (event) => {
  if (activeTab !== "Lista") setTab("Lista");
  renderTracks(event.target.value);
});
$("#play").addEventListener("click", () => { isPlaying = !isPlaying; updatePlayer(); });
$("#next").addEventListener("click", () => selectTrack(currentTrack + 1));
$("#previous").addEventListener("click", () => selectTrack(currentTrack - 1));
$("#shuffle").addEventListener("click", (event) => event.currentTarget.classList.toggle("selected"));
$("#repeat").addEventListener("click", (event) => event.currentTarget.classList.toggle("selected"));
$("#favorite").addEventListener("click", (event) => {
  const button = event.currentTarget;
  button.classList.toggle("is-favorite");
  button.textContent = button.classList.contains("is-favorite") ? "♥" : "♡";
});
progress.addEventListener("input", () => {
  elapsed = Number(progress.value);
  progress.style.setProperty("--fill", `${(elapsed / Number(progress.max)) * 100}%`);
  $("#elapsed").textContent = formatTime(elapsed);
});
document.querySelectorAll(".tab").forEach((button) => button.addEventListener("click", () => setTab(button.dataset.tab)));
document.querySelectorAll(".nav-item").forEach((button) => button.addEventListener("click", () => {
  document.querySelectorAll(".nav-item").forEach((item) => item.classList.remove("active"));
  button.classList.add("active");
  if (button.dataset.view !== "Inicio") {
    $("#track-title").textContent = button.dataset.view;
    $("#track-artist").textContent = "Explora tu colección";
  } else updatePlayer();
}));
document.querySelectorAll(".recent-item").forEach((button) => button.addEventListener("click", () => {
  if (button.dataset.track !== undefined) selectTrack(Number(button.dataset.track));
  else document.querySelector('[data-view="Vídeos"]').click();
}));
$("#up-next").addEventListener("click", () => $(".up-next").classList.toggle("collapsed"));
renderTracks();
renderUpcoming();
setInterval(() => {
  if (!isPlaying) return;
  elapsed += 1;
  if (elapsed >= tracks[currentTrack].seconds) selectTrack(currentTrack + 1);
  else updatePlayer();
}, 1000);
