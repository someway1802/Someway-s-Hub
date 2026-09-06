/* ==========================================================================
   Someway's Hub - Ultra Aspect Ratio Media Player Engine
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  // DOM Elements
  const video = document.getElementById('video-player');
  const videoViewport = document.getElementById('video-viewport');
  const playerStage = document.getElementById('player-stage');
  const fileInput = document.getElementById('file-input');
  const subInput = document.getElementById('sub-input');
  const dropzone = document.getElementById('dropzone');
  const emptyState = document.getElementById('empty-state');
  const activeFilename = document.getElementById('active-filename');
  const currentAspectBadge = document.getElementById('current-aspect-badge');
  const aspectToast = document.getElementById('aspect-toast');
  const toastText = document.getElementById('toast-text');
  const statusDot = document.querySelector('.status-dot');
  const subtitleOverlay = document.getElementById('subtitle-overlay');
  const ambientGlow = document.getElementById('ambient-glow');

  // Controls UI
  const controlsOverlay = document.getElementById('controls-overlay');
  const btnPlayPause = document.getElementById('btn-play-pause');
  const iconPlay = document.getElementById('icon-play');
  const iconPause = document.getElementById('icon-pause');
  const btnRewind = document.getElementById('btn-rewind');
  const btnForward = document.getElementById('btn-forward');
  const btnForward135 = document.getElementById('btn-forward-135');
  const btnMute = document.getElementById('btn-mute');
  const iconVolumeHigh = document.getElementById('icon-volume-high');
  const iconVolumeMute = document.getElementById('icon-volume-mute');
  const volumeSlider = document.getElementById('volume-slider');
  const timeCurrent = document.getElementById('time-current');
  const timeDuration = document.getElementById('time-duration');

  // Seekbar
  const seekbarContainer = document.getElementById('seekbar-container');
  const seekbarFill = document.getElementById('seekbar-fill');
  const seekbarBuffer = document.getElementById('seekbar-buffer');
  const seekbarHandle = document.getElementById('seekbar-handle');
  const seekbarTooltip = document.getElementById('seekbar-tooltip');

  // Quick Aspect & Speed Dropdowns
  const btnAspectQuick = document.getElementById('btn-aspect-quick');
  const aspectQuickLabel = document.getElementById('aspect-quick-label');
  const aspectDropdownMenu = document.getElementById('aspect-dropdown-menu');
  const btnSpeed = document.getElementById('btn-speed');
  const speedLabel = document.getElementById('speed-label');
  const speedDropdownMenu = document.getElementById('speed-dropdown-menu');
  const btnPip = document.getElementById('btn-pip');
  const btnFullscreen = document.getElementById('btn-fullscreen');

  // Drawer Controls
  const sideDrawer = document.getElementById('side-drawer');
  const btnToggleDrawer = document.getElementById('btn-toggle-drawer');
  const btnCloseDrawer = document.getElementById('btn-close-drawer');
  const btnCustomScaleOpen = document.getElementById('btn-custom-scale-open');
  const aspectRadioInputs = document.querySelectorAll('input[name="aspect-mode"]');

  // Stats Elements
  const statScreenRes = document.getElementById('stat-screen-res');
  const statScreenAspect = document.getElementById('stat-screen-aspect');
  const statVideoRes = document.getElementById('stat-video-res');
  const statVideoAspect = document.getElementById('stat-video-aspect');

  // Sliders & Controls
  const sliderScaleX = document.getElementById('slider-scale-x');
  const sliderScaleY = document.getElementById('slider-scale-y');
  const valScaleX = document.getElementById('val-scale-x');
  const valScaleY = document.getElementById('val-scale-y');
  const sliderPanX = document.getElementById('slider-pan-x');
  const sliderPanY = document.getElementById('slider-pan-y');
  const valPanX = document.getElementById('val-pan-x');
  const valPanY = document.getElementById('val-pan-y');
  const btnResetScaling = document.getElementById('btn-reset-scaling');

  const sliderBrightness = document.getElementById('slider-brightness');
  const sliderContrast = document.getElementById('slider-contrast');
  const sliderSaturation = document.getElementById('slider-saturation');
  const valBrightness = document.getElementById('val-brightness');
  const valContrast = document.getElementById('val-contrast');
  const valSaturation = document.getElementById('val-saturation');
  const btnRotate = document.getElementById('btn-rotate');
  const btnFlipH = document.getElementById('btn-flip-h');
  const btnFlipV = document.getElementById('btn-flip-v');
  const btnResetFilters = document.getElementById('btn-reset-filters');

  const sliderGain = document.getElementById('slider-gain');
  const valGain = document.getElementById('val-gain');
  const toggleAmbient = document.getElementById('toggle-ambient');

  // Modals & Samples
  const btnSamples = document.getElementById('btn-samples');
  const emptyDemoBtn = document.getElementById('empty-demo-btn');
  const emptyOpenBtn = document.getElementById('empty-open-btn');
  const modalSamples = document.getElementById('modal-samples');
  const btnHotkeys = document.getElementById('btn-hotkeys');
  const modalHotkeys = document.getElementById('modal-hotkeys');
  const closeModals = document.querySelectorAll('.close-modal');

  // Player State
  const state = {
    aspectMode: 'stretch-screen', // Stremio screen match stretch mode by default
    scaleX: 100,
    scaleY: 100,
    panX: 0,
    panY: 0,
    brightness: 100,
    contrast: 100,
    saturation: 100,
    rotation: 0,
    flipH: 1,
    flipV: 1,
    gainBoost: 100,
    ambient: true,
    subtitles: []
  };

  // Audio Gain Node (Without Canvas Visualizer overlay)
  let audioCtx = null;
  let audioSource = null;
  let gainNode = null;

  // Controls Idle Timer
  let controlsTimeout = null;

  // ==========================================================================
  // Initialization & Screen Aspect Calculation
  // ==========================================================================

  function initScreenStats() {
    const sw = window.innerWidth;
    const sh = window.innerHeight;
    const ratio = (sw / sh).toFixed(2);
    statScreenRes.textContent = `${sw} x ${sh}`;
    statScreenAspect.textContent = `${ratio}:1 (${getAspectName(sw, sh)})`;
  }

  function getAspectName(w, h) {
    const r = w / h;
    if (Math.abs(r - 1.777) < 0.08) return '16:9';
    if (Math.abs(r - 1.333) < 0.08) return '4:3';
    if (Math.abs(r - 2.333) < 0.12) return '21:9 Ultrawide';
    if (Math.abs(r - 0.5625) < 0.08) return '9:16 Vertical';
    if (Math.abs(r - 1) < 0.05) return '1:1';
    return `${r.toFixed(2)}:1`;
  }

  window.addEventListener('resize', () => {
    initScreenStats();
    if (state.aspectMode === 'stretch-screen') {
      applyAspectTransform();
    }
  });
  initScreenStats();

  // ==========================================================================
  // File & Video Loading Engine & Playlist Island
  // ==========================================================================

  let playlist = [];
  let currentPlaylistIndex = -1;

  const btnPlaylistHeader = document.getElementById('btn-playlist-header');
  const playlistIslandPanel = document.getElementById('playlist-island-panel');
  const btnClosePlaylist = document.getElementById('btn-close-playlist');
  const playlistFileInput = document.getElementById('playlist-file-input');
  const playlistItemsContainer = document.getElementById('playlist-items-container');
  const playlistCountBadge = document.getElementById('playlist-count-badge');
  const playlistTotalText = document.getElementById('playlist-total-text');

  function toggleSettingsPanel() {
    if (!sideDrawer) return;
    const willOpen = !sideDrawer.classList.contains('open');
    if (willOpen && playlistIslandPanel) {
      playlistIslandPanel.classList.remove('active');
    }
    sideDrawer.classList.toggle('open');
  }

  function togglePlaylistPanel() {
    if (!playlistIslandPanel) return;
    const willOpen = !playlistIslandPanel.classList.contains('active');
    if (willOpen && sideDrawer) {
      sideDrawer.classList.remove('open');
    }
    playlistIslandPanel.classList.toggle('active');
  }

  if (btnPlaylistHeader && playlistIslandPanel) {
    btnPlaylistHeader.addEventListener('click', togglePlaylistPanel);
  }
  if (btnClosePlaylist && playlistIslandPanel) {
    btnClosePlaylist.addEventListener('click', () => playlistIslandPanel.classList.remove('active'));
  }
  if (playlistFileInput) {
    playlistFileInput.addEventListener('change', (e) => {
      addFilesToPlaylist(e.target.files, true);
    });
  }

  function addFilesToPlaylist(fileList, playIfFirst = true) {
    if (!fileList || fileList.length === 0) return;
    let addedCount = 0;
    Array.from(fileList).forEach(file => {
      if (file.type.startsWith('video/') || file.name.match(/\.(mp4|webm|mkv|avi|mov|wmv|flv|m4v)$/i)) {
        const objectUrl = URL.createObjectURL(file);
        playlist.push({ name: file.name, url: objectUrl, file: file });
        addedCount++;
      }
    });

    if (playlist.length > 0) {
      refreshPlaylistUI();
      if (currentPlaylistIndex === -1 || playIfFirst) {
        playPlaylistItem(playlist.length - (addedCount > 0 ? addedCount : 1));
      }
    }
  }

  function playPlaylistItem(index) {
    if (index < 0 || index >= playlist.length) return;
    currentPlaylistIndex = index;
    const item = playlist[index];
    loadVideoSource(item.url, item.name, item.file);
    refreshPlaylistUI();
  }

  function removePlaylistItem(index) {
    if (index < 0 || index >= playlist.length) return;
    const wasPlaying = (currentPlaylistIndex === index);
    playlist.splice(index, 1);
    if (playlist.length === 0) {
      currentPlaylistIndex = -1;
      video.pause();
      video.src = '';
      emptyState.style.display = 'flex';
      activeFilename.textContent = 'No video loaded';
      statusDot.classList.remove('active');
    } else {
      if (wasPlaying) {
        const nextIdx = index < playlist.length ? index : playlist.length - 1;
        playPlaylistItem(nextIdx);
      } else if (currentPlaylistIndex > index) {
        currentPlaylistIndex--;
      }
    refreshPlaylistUI();
  }

  function movePlaylistItem(fromIndex, toIndex) {
    if (fromIndex < 0 || fromIndex >= playlist.length) return;
    if (toIndex < 0 || toIndex >= playlist.length) return;
    if (fromIndex === toIndex) return;

    const item = playlist.splice(fromIndex, 1)[0];
    playlist.splice(toIndex, 0, item);

    if (currentPlaylistIndex === fromIndex) {
      currentPlaylistIndex = toIndex;
    } else if (currentPlaylistIndex > fromIndex && currentPlaylistIndex <= toIndex) {
      currentPlaylistIndex--;
    } else if (currentPlaylistIndex < fromIndex && currentPlaylistIndex >= toIndex) {
      currentPlaylistIndex++;
    }

    refreshPlaylistUI();
    showToast(`Reordered: ${item.name}`);
  }

  function refreshPlaylistUI() {
    if (playlistCountBadge) playlistCountBadge.textContent = playlist.length;
    if (playlistTotalText) playlistTotalText.textContent = `${playlist.length} video${playlist.length === 1 ? '' : 's'}`;
    if (!playlistItemsContainer) return;

    playlistItemsContainer.innerHTML = '';
    if (playlist.length === 0) {
      playlistItemsContainer.innerHTML = `
        <div class="playlist-empty-state">
          <p>No videos uploaded yet</p>
          <span class="playlist-empty-hint">Click "+ Add" or drop multiple MP4 files here</span>
        </div>`;
      return;
    }

    playlist.forEach((item, i) => {
      const isCurrent = (i === currentPlaylistIndex);
      const card = document.createElement('div');
      card.className = `playlist-item-card ${isCurrent ? 'active' : ''}`;
      card.setAttribute('draggable', 'true');
      card.dataset.index = i;

      let moveUpHtml = i > 0 ? `<button class="playlist-item-move btn-move-up" title="Move Up">▲</button>` : '';
      let moveDownHtml = i < playlist.length - 1 ? `<button class="playlist-item-move btn-move-down" title="Move Down">▼</button>` : '';

      card.innerHTML = `
        <div class="playlist-item-left">
          <span class="drag-handle" title="Drag up/down to reorder">⣿</span>
          <span class="playlist-item-num">${isCurrent ? '▶' : (i + 1) + '.'}</span>
          <span class="playlist-item-name" title="${item.name}">${item.name}</span>
        </div>
        <div class="playlist-item-actions">
          ${moveUpHtml}
          ${moveDownHtml}
          <button class="playlist-item-remove" title="Remove video">✕</button>
        </div>
      `;

      card.addEventListener('dragstart', (e) => {
        card.classList.add('dragging');
        e.dataTransfer.setData('text/plain', i);
        e.dataTransfer.effectAllowed = 'move';
      });

      card.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        card.classList.add('drag-target');
      });

      card.addEventListener('dragleave', () => {
        card.classList.remove('drag-target');
      });

      card.addEventListener('drop', (e) => {
        e.preventDefault();
        card.classList.remove('drag-target');
        const fromIdx = parseInt(e.dataTransfer.getData('text/plain'));
        if (!isNaN(fromIdx)) {
          movePlaylistItem(fromIdx, i);
        }
      });

      card.addEventListener('dragend', () => {
        card.classList.remove('dragging');
      });

      card.addEventListener('click', (e) => {
        if (e.target.classList.contains('playlist-item-remove')) {
          e.stopPropagation();
          removePlaylistItem(i);
        } else if (e.target.classList.contains('btn-move-up')) {
          e.stopPropagation();
          movePlaylistItem(i, i - 1);
        } else if (e.target.classList.contains('btn-move-down')) {
          e.stopPropagation();
          movePlaylistItem(i, i + 1);
        } else {
          playPlaylistItem(i);
        }
      });

      playlistItemsContainer.appendChild(card);
    });
  }

  function loadVideoSource(sourceUrl, titleName, fileObj) {
    let playUrl = sourceUrl;

    // Enhanced Matroska (.mkv) Container Handling Engine
    if (fileObj && (fileObj.name.toLowerCase().endsWith('.mkv') || fileObj.type === 'video/x-matroska' || fileObj.type === 'video/mkv' || !fileObj.type)) {
      try {
        // Matroska (.mkv) and WebM (.webm) share identical EBML binary container architecture.
        // Wrapping MKV File in a 'video/webm' Blob allows Chromium/Edge's native demuxer to parse Matroska streams directly.
        const mkvWebmBlob = new Blob([fileObj], { type: 'video/webm' });
        playUrl = URL.createObjectURL(mkvWebmBlob);
        inspectMKVFile(fileObj);
      } catch (err) {
        console.warn('MKV Blob conversion fallback to original URL:', err);
      }
    }

    video.src = playUrl;
    activeFilename.textContent = titleName || 'Loaded Video';
    statusDot.classList.add('active');
    emptyState.style.display = 'none';

    video.play().then(() => {
      updatePlayPauseIcon();
      setupAudioGain();
    }).catch(err => {
      console.log('Autoplay prevented or paused:', err);
      updatePlayPauseIcon();
    });

    showToast(`Loaded: ${titleName}`);
  }

  function inspectMKVFile(file) {
    if (!file) return;
    const reader = new FileReader();
    const slice = file.slice(0, 65536);
    reader.onload = (e) => {
      try {
        const buffer = new Uint8Array(e.target.result);
        const mkvInfo = parseMatroskaHeader(buffer);
        if (mkvInfo) {
          showToast(`🎬 MKV Matroska: ${mkvInfo.videoCodec} / ${mkvInfo.audioCodec}`);
        } else {
          showToast(`🎬 MKV Matroska Container Loaded`);
        }
      } catch (err) {
        showToast(`🎬 MKV File Loaded`);
      }
    };
    reader.readAsArrayBuffer(slice);
  }

  function parseMatroskaHeader(buf) {
    if (buf[0] !== 0x1A || buf[1] !== 0x45 || buf[2] !== 0xDF || buf[3] !== 0xA3) {
      return null;
    }
    let videoCodec = 'H.264 / WebM Video';
    let audioCodec = 'AAC / Opus Audio';
    const str = String.fromCharCode.apply(null, buf);

    if (str.includes('V_MPEG4/ISO/AVC')) videoCodec = 'H.264 (AVC)';
    else if (str.includes('V_MPEGH/ISO/HEVC')) videoCodec = 'H.265 (HEVC)';
    else if (str.includes('V_VP9')) videoCodec = 'VP9';
    else if (str.includes('V_VP8')) videoCodec = 'VP8';
    else if (str.includes('V_AV1')) videoCodec = 'AV1';

    if (str.includes('A_AAC')) audioCodec = 'AAC';
    else if (str.includes('A_OPUS')) audioCodec = 'Opus';
    else if (str.includes('A_VORBIS')) audioCodec = 'Vorbis';
    else if (str.includes('A_AC3')) audioCodec = 'AC3';
    else if (str.includes('A_EAC3')) audioCodec = 'EAC3';
    else if (str.includes('A_MPEG/L3')) audioCodec = 'MP3';

    return { videoCodec, audioCodec };
  }

  fileInput.addEventListener('change', (e) => {
    addFilesToPlaylist(e.target.files, true);
  });

  emptyOpenBtn.addEventListener('click', () => fileInput.click());
  emptyDemoBtn.addEventListener('click', () => modalSamples.classList.add('show'));

  // Drag & Drop Handlers
  window.addEventListener('dragover', (e) => {
    e.preventDefault();
    dropzone.classList.add('drag-over');
  });

  window.addEventListener('dragleave', (e) => {
    if (e.clientX <= 0 || e.clientY <= 0 || e.clientX >= window.innerWidth || e.clientY >= window.innerHeight) {
      dropzone.classList.remove('drag-over');
    }
  });

  window.addEventListener('drop', (e) => {
    e.preventDefault();
    dropzone.classList.remove('drag-over');
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      addFilesToPlaylist(e.dataTransfer.files, true);
    }
  });

  // Video Loaded Metadata Handler
  video.addEventListener('loadedmetadata', () => {
    const vw = video.videoWidth;
    const vh = video.videoHeight;
    const ratio = (vw / vh).toFixed(2);
    statVideoRes.textContent = `${vw} x ${vh}`;
    statVideoAspect.textContent = `${ratio}:1 (${getAspectName(vw, vh)})`;

    timeDuration.textContent = formatTime(video.duration);
    applyAspectTransform();
  });

  video.addEventListener('playing', () => {
    setTimeout(verifyVideoPictureDecoding, 1200);
  });

  function verifyVideoPictureDecoding() {
    if (!video.src || video.paused) return;
    const hasDimensions = (video.videoWidth > 0 && video.videoHeight > 0);
    const decodedFrames = video.webkitDecodedFrameCount;

    if (!hasDimensions || (decodedFrames !== undefined && decodedFrames === 0)) {
      showToast('⚠️ Black Screen: HEVC / 10-Bit MKV Video Codec Unsupported');
    }
  }

  video.addEventListener('error', (e) => {
    console.error('Video error:', video.error);
    showToast('⚠️ Codec Unsupported: Try an H.264 MP4 or WebM video file');
  });

  video.addEventListener('ended', () => {
    if (currentPlaylistIndex >= 0 && currentPlaylistIndex < playlist.length - 1) {
      playPlaylistItem(currentPlaylistIndex + 1);
    } else {
      updatePlayPauseIcon();
      showControls();
    }
  });

  // ==========================================================================
  // Stremio Aspect Ratio & Stretch Engine
  // ==========================================================================

  function applyAspectTransform() {
    video.className = '';

    const mode = state.aspectMode;
    let badgeText = 'Fit';

    if (mode === 'stretch-screen') {
      video.classList.add('mode-stretch-screen');
      badgeText = 'STRETCH SCREEN (100%)';
    } else if (mode === 'fit') {
      video.classList.add('mode-fit');
      badgeText = 'Fit (16:9 / Native)';
    } else if (mode === 'stretch-fill') {
      video.classList.add('mode-stretch-fill');
      badgeText = 'Stretch Fill';
    } else if (mode === 'cover') {
      video.classList.add('mode-cover');
      badgeText = 'Cover (Crop)';
    } else if (mode.startsWith('preset-')) {
      const preset = mode.replace('preset-', '');
      badgeText = `Preset ${preset.replace('-', ':')}`;
      applyPresetRatio(preset);
    }

    aspectQuickLabel.textContent = badgeText.toUpperCase();
    currentAspectBadge.textContent = badgeText;

    const scaleXFactor = state.scaleX / 100;
    const scaleYFactor = state.scaleY / 100;

    video.style.transform = `
      translate(${state.panX}px, ${state.panY}px)
      scale(${scaleXFactor}, ${scaleYFactor})
      rotate(${state.rotation}deg)
      scaleX(${state.flipH})
      scaleY(${state.flipV})
    `;

    video.style.filter = `
      brightness(${state.brightness}%)
      contrast(${state.contrast}%)
      saturate(${state.saturation}%)
    `;

    aspectRadioInputs.forEach(radio => {
      radio.checked = (radio.value === mode);
    });
  }

  function applyPresetRatio(preset) {
    let targetRatio = 16 / 9;
    if (preset === '4-3') targetRatio = 4 / 3;
    if (preset === '21-9') targetRatio = 21 / 9;
    if (preset === '1-1') targetRatio = 1 / 1;
    if (preset === '9-16') targetRatio = 9 / 16;

    if (video.videoWidth && video.videoHeight) {
      const nativeRatio = video.videoWidth / video.videoHeight;
      const stretchFactorX = (targetRatio / nativeRatio) * 100;
      state.scaleX = Math.round(stretchFactorX);
      sliderScaleX.value = state.scaleX;
      valScaleX.textContent = `${state.scaleX}%`;
    }
  }

  function setAspectMode(mode) {
    state.aspectMode = mode;
    applyAspectTransform();
    showToast(`Aspect Ratio: ${mode.toUpperCase().replace('-', ' ')}`);
  }

  // Single Cycle Aspect Ratio Button
  btnAspectQuick.addEventListener('click', (e) => {
    e.stopPropagation();
    cycleAspectMode();
  });

  document.querySelectorAll('#aspect-dropdown-menu .dropdown-item, #aspect-dropdown-menu .btn-preset').forEach(btn => {
    btn.addEventListener('click', () => {
      const mode = btn.dataset.mode;
      if (mode) {
        setAspectMode(mode);
        document.querySelectorAll('#aspect-dropdown-menu .dropdown-item').forEach(i => i.classList.remove('active'));
        btn.classList.add('active');
      }
      aspectDropdownMenu.classList.remove('show');
    });
  });

  btnCustomScaleOpen.addEventListener('click', () => {
    sideDrawer.classList.add('open');
    aspectDropdownMenu.classList.remove('show');
  });

  function cycleAspectMode() {
    const modes = ['stretch-screen', 'fit', 'cover', 'stretch-fill', 'preset-21-9', 'preset-4-3'];
    const currentIndex = modes.indexOf(state.aspectMode);
    const nextIndex = (currentIndex + 1) % modes.length;
    setAspectMode(modes[nextIndex]);
  }

  aspectRadioInputs.forEach(radio => {
    radio.addEventListener('change', (e) => {
      setAspectMode(e.target.value);
    });
  });

  // Scale & Pan Sliders
  sliderScaleX.addEventListener('input', (e) => {
    state.scaleX = parseInt(e.target.value);
    valScaleX.textContent = `${state.scaleX}%`;
    applyAspectTransform();
  });

  sliderScaleY.addEventListener('input', (e) => {
    state.scaleY = parseInt(e.target.value);
    valScaleY.textContent = `${state.scaleY}%`;
    applyAspectTransform();
  });

  sliderPanX.addEventListener('input', (e) => {
    state.panX = parseInt(e.target.value);
    valPanX.textContent = `${state.panX}px`;
    applyAspectTransform();
  });

  sliderPanY.addEventListener('input', (e) => {
    state.panY = parseInt(e.target.value);
    valPanY.textContent = `${state.panY}px`;
    applyAspectTransform();
  });

  btnResetScaling.addEventListener('click', () => {
    state.scaleX = 100;
    state.scaleY = 100;
    state.panX = 0;
    state.panY = 0;
    sliderScaleX.value = 100;
    sliderScaleY.value = 100;
    sliderPanX.value = 0;
    sliderPanY.value = 0;
    valScaleX.textContent = '100%';
    valScaleY.textContent = '100%';
    valPanX.textContent = '0px';
    valPanY.textContent = '0px';
    applyAspectTransform();
    showToast('Reset Scaling & Pan');
  });

  // Picture Filters Sliders
  sliderBrightness.addEventListener('input', (e) => {
    state.brightness = parseInt(e.target.value);
    valBrightness.textContent = `${state.brightness}%`;
    applyAspectTransform();
  });

  sliderContrast.addEventListener('input', (e) => {
    state.contrast = parseInt(e.target.value);
    valContrast.textContent = `${state.contrast}%`;
    applyAspectTransform();
  });

  sliderSaturation.addEventListener('input', (e) => {
    state.saturation = parseInt(e.target.value);
    valSaturation.textContent = `${state.saturation}%`;
    applyAspectTransform();
  });

  btnRotate.addEventListener('click', () => {
    state.rotation = (state.rotation + 90) % 360;
    btnRotate.querySelector('span').textContent = `Rotate (${state.rotation}°)`;
    applyAspectTransform();
  });

  btnFlipH.addEventListener('click', () => {
    state.flipH = state.flipH === 1 ? -1 : 1;
    btnFlipH.classList.toggle('active', state.flipH === -1);
    applyAspectTransform();
  });

  btnFlipV.addEventListener('click', () => {
    state.flipV = state.flipV === 1 ? -1 : 1;
    btnFlipV.classList.toggle('active', state.flipV === -1);
    applyAspectTransform();
  });

  btnResetFilters.addEventListener('click', () => {
    state.brightness = 100;
    state.contrast = 100;
    state.saturation = 100;
    state.rotation = 0;
    state.flipH = 1;
    state.flipV = 1;
    sliderBrightness.value = 100;
    sliderContrast.value = 100;
    sliderSaturation.value = 100;
    valBrightness.textContent = '100%';
    valContrast.textContent = '100%';
    valSaturation.textContent = '100%';
    btnRotate.querySelector('span').textContent = 'Rotate (0°)';
    btnFlipH.classList.remove('active');
    btnFlipV.classList.remove('active');
    applyAspectTransform();
    showToast('Reset Picture Filters');
  });

  toggleAmbient.addEventListener('change', (e) => {
    state.ambient = e.target.checked;
    ambientGlow.classList.toggle('active', state.ambient && !video.paused);
  });

  btnToggleDrawer.addEventListener('click', toggleSettingsPanel);
  btnCloseDrawer.addEventListener('click', () => sideDrawer.classList.remove('open'));

  // ==========================================================================
  // Video Controls Logic
  // ==========================================================================

  function togglePlayPause() {
    if (!video.src) return;
    if (video.paused) {
      video.play();
      if (state.ambient) ambientGlow.classList.add('active');
    } else {
      video.pause();
      ambientGlow.classList.remove('active');
    }
    updatePlayPauseIcon();
  }

  function updatePlayPauseIcon() {
    if (video.paused) {
      iconPlay.style.display = 'block';
      iconPause.style.display = 'none';
    } else {
      iconPlay.style.display = 'none';
      iconPause.style.display = 'block';
    }
  }

  btnPlayPause.addEventListener('click', togglePlayPause);

  playerStage.addEventListener('click', (e) => {
    if (e.target.closest('.controls-overlay') || e.target.closest('.navbar') || e.target.closest('.modal-backdrop') || e.target.closest('.side-drawer')) {
      return;
    }
    togglePlayPause();
  });

  if (btnRewind) {
    btnRewind.addEventListener('click', () => {
      if (!video.duration) return;
      video.currentTime = Math.max(0, video.currentTime - 10);
      showToast('Rewind 10s (<)');
    });
  }

  if (btnForward) {
    btnForward.addEventListener('click', () => {
      if (!video.duration) return;
      video.currentTime = Math.min(video.duration, video.currentTime + 10);
      showToast('Forward 10s (>)');
    });
  }

  if (btnForward135) {
    btnForward135.addEventListener('click', () => {
      if (!video.duration) return;
      video.currentTime = Math.min(video.duration, video.currentTime + 95);
      showToast('Forward 1:35 (>>)');
    });
  }

  function updateVolumeIcon() {
    if (video.muted || video.volume === 0) {
      iconVolumeHigh.style.display = 'none';
      iconVolumeMute.style.display = 'block';
    } else {
      iconVolumeHigh.style.display = 'block';
      iconVolumeMute.style.display = 'none';
    }
  }

  let lastWebVolume = 1;

  btnMute.addEventListener('click', () => {
    if (!video.muted && video.volume > 0) {
      lastWebVolume = video.volume;
      video.volume = 0;
      volumeSlider.value = 0;
      video.muted = true;
    } else {
      const restoreVal = lastWebVolume > 0 ? lastWebVolume : 1;
      video.volume = restoreVal;
      volumeSlider.value = restoreVal;
      video.muted = false;
    }
    updateVolumeIcon();
  });

  volumeSlider.addEventListener('input', (e) => {
    video.volume = parseFloat(e.target.value);
    video.muted = (video.volume === 0);
    updateVolumeIcon();
  });

  video.addEventListener('timeupdate', () => {
    if (isNaN(video.duration)) return;
    timeCurrent.textContent = formatTime(video.currentTime);
    const pct = (video.currentTime / video.duration) * 100;
    seekbarFill.style.width = `${pct}%`;
    seekbarHandle.style.left = `${pct}%`;

    if (video.buffered.length > 0) {
      const bufPct = (video.buffered.end(video.buffered.length - 1) / video.duration) * 100;
      seekbarBuffer.style.width = `${bufPct}%`;
    }

    updateActiveSubtitle(video.currentTime);
  });

  seekbarContainer.addEventListener('click', (e) => {
    const rect = seekbarContainer.getBoundingClientRect();
    const pos = (e.clientX - rect.left) / rect.width;
    video.currentTime = pos * video.duration;
  });

  seekbarContainer.addEventListener('mousemove', (e) => {
    const rect = seekbarContainer.getBoundingClientRect();
    const pos = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    seekbarTooltip.style.left = `${pos * 100}%`;
    if (video.duration) {
      seekbarTooltip.textContent = formatTime(pos * video.duration);
    }
  });

  btnSpeed.addEventListener('click', (e) => {
    e.stopPropagation();
    speedDropdownMenu.classList.toggle('show');
    if (aspectDropdownMenu) aspectDropdownMenu.classList.remove('show');
  });

  const btnLang = document.getElementById('btn-lang');
  if (btnLang) {
    btnLang.addEventListener('click', () => showToast('Audio Track: Default (Stereo)'));
  }

  const btnCast = document.getElementById('btn-cast');
  if (btnCast) {
    btnCast.addEventListener('click', () => showToast('Searching for Cast devices...'));
  }



  document.querySelectorAll('.speed-opt').forEach(btn => {
    btn.addEventListener('click', () => {
      const speed = parseFloat(btn.dataset.speed);
      video.playbackRate = speed;
      if (speedLabel) speedLabel.textContent = `${speed}x`;
      document.querySelectorAll('.speed-opt').forEach(i => i.classList.remove('active'));
      btn.classList.add('active');
      speedDropdownMenu.classList.remove('show');
      showToast(`Playback Speed: ${speed}x`);
    });
  });

  if (btnPip) {
    btnPip.addEventListener('click', async () => {
      try {
        if (document.pictureInPictureElement) {
          await document.exitPictureInPicture();
        } else {
          await video.requestPictureInPicture();
        }
      } catch (err) {
        console.log('PiP failed:', err);
      }
    });
  }

  btnFullscreen.addEventListener('click', toggleFullscreen);

  function toggleFullscreen() {
    if (!document.fullscreenElement) {
      playerStage.requestFullscreen().catch(err => {
        console.log('Fullscreen error:', err);
      });
    } else {
      document.exitFullscreen();
    }
  document.addEventListener('click', () => {
    aspectDropdownMenu.classList.remove('show');
    speedDropdownMenu.classList.remove('show');
  });

  // Idle controls hider (2-second auto-hide)
  const navbar = document.querySelector('.navbar');
  playerStage.addEventListener('mousemove', resetControlsTimer);
  playerStage.addEventListener('mouseleave', () => {
    if (!video.paused) {
      controlsOverlay.classList.add('hide-controls');
      if (navbar) navbar.classList.add('hide-controls');
    }
  });

  function resetControlsTimer() {
    controlsOverlay.classList.remove('hide-controls');
    if (navbar) navbar.classList.remove('hide-controls');
    clearTimeout(controlsTimeout);
    if (!video.paused) {
      controlsTimeout = setTimeout(() => {
        controlsOverlay.classList.add('hide-controls');
        if (navbar) navbar.classList.add('hide-controls');
      }, 2000); // 2 seconds!
    }
  }

  // ==========================================================================
  // Web Audio Gain Boost (Without Visualizer Overlay)
  // ==========================================================================

  function setupAudioGain() {
    if (audioCtx) return;
    try {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      audioCtx = new AudioCtx();
      audioSource = audioCtx.createMediaElementSource(video);
      gainNode = audioCtx.createGain();
      audioSource.connect(gainNode);
      gainNode.connect(audioCtx.destination);
    } catch (e) {
      console.warn('AudioContext setup skipped:', e);
    }
  }

  sliderGain.addEventListener('input', (e) => {
    state.gainBoost = parseInt(e.target.value);
    valGain.textContent = `${state.gainBoost}%`;
    if (gainNode) {
      gainNode.gain.value = state.gainBoost / 100;
    }
  });

  // ==========================================================================
  // Subtitle Parser (.vtt / .srt)
  // ==========================================================================

  subInput.addEventListener('change', (e) => {
    const file = e.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target.result;
      state.subtitles = parseSRTorVTT(text);
      showToast(`Loaded Subtitles: ${file.name}`);
    };
    reader.readAsText(file);
  });

  function parseSRTorVTT(text) {
    const cues = [];
    const blocks = text.split(/\n\r?\n/);

    blocks.forEach(block => {
      const lines = block.trim().split(/\r?\n/);
      let timeLine = '';
      let cueText = '';

      lines.forEach(line => {
        if (line.includes('-->')) {
          timeLine = line;
        } else if (timeLine && !line.match(/^\d+$/)) {
          cueText += (cueText ? '<br>' : '') + line;
        }
      });

      if (timeLine && cueText) {
        const times = timeLine.split('-->');
        const start = parseTimestamp(times[0].trim());
        const end = parseTimestamp(times[1].trim());
        cues.push({ start, end, text: cueText });
      }
    });

    return cues;
  }

  function parseTimestamp(ts) {
    const parts = ts.replace(',', '.').split(':');
    if (parts.length === 3) {
      return parseFloat(parts[0]) * 3600 + parseFloat(parts[1]) * 60 + parseFloat(parts[2]);
    } else if (parts.length === 2) {
      return parseFloat(parts[0]) * 60 + parseFloat(parts[1]);
    }
    return 0;
  }

  function updateActiveSubtitle(currentTime) {
    if (!state.subtitles.length) {
      subtitleOverlay.innerHTML = '';
      return;
    }
    const currentCue = state.subtitles.find(cue => currentTime >= cue.start && currentTime <= cue.end);
    if (currentCue) {
      subtitleOverlay.innerHTML = currentCue.text;
      subtitleOverlay.style.display = 'block';
    } else {
      subtitleOverlay.style.display = 'none';
    }
  }

  // ==========================================================================
  // Sample Video Selection
  // ==========================================================================

  btnSamples.addEventListener('click', () => modalSamples.classList.add('show'));

  document.querySelectorAll('.sample-card').forEach(card => {
    card.addEventListener('click', () => {
      const url = card.dataset.url;
      const name = card.dataset.name;
      loadVideoSource(url, name);
      modalSamples.classList.remove('show');
    });
  });

  btnHotkeys.addEventListener('click', () => modalHotkeys.classList.add('show'));

  closeModals.forEach(btn => {
    btn.addEventListener('click', () => {
      modalSamples.classList.remove('show');
      modalHotkeys.classList.remove('show');
    });
  });

  window.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal-backdrop')) {
      e.target.classList.remove('show');
    }
  });

  // ==========================================================================
  // Global Hotkeys Listener
  // ==========================================================================

  window.addEventListener('keydown', (e) => {
    if (['INPUT', 'TEXTAREA', 'SELECT'].includes(document.activeElement.tagName)) return;

    switch (e.code) {
      case 'Space':
      case 'KeyK':
        e.preventDefault();
        togglePlayPause();
        break;
      case 'KeyF':
        e.preventDefault();
        toggleFullscreen();
        break;
      case 'KeyA':
        e.preventDefault();
        cycleAspectMode();
        break;
      case 'KeyM':
        e.preventDefault();
        video.muted = !video.muted;
        updateVolumeIcon();
        break;
      case 'Comma':
      case 'KeyJ':
        e.preventDefault();
        if (btnRewind) btnRewind.click();
        break;
      case 'Period':
      case 'KeyL':
        e.preventDefault();
        if (btnForward) btnForward.click();
        break;
      case 'BracketRight':
      case 'KeyN':
        e.preventDefault();
        if (btnForward135) btnForward135.click();
        break;
      case 'ArrowLeft':
        e.preventDefault();
        video.currentTime = Math.max(0, video.currentTime - 5);
        break;
      case 'ArrowRight':
        e.preventDefault();
        video.currentTime = Math.min(video.duration, video.currentTime + 5);
        break;
      case 'ArrowUp':
        e.preventDefault();
        video.volume = Math.min(1, video.volume + 0.1);
        volumeSlider.value = video.volume;
        updateVolumeIcon();
        break;
      case 'ArrowDown':
        e.preventDefault();
        video.volume = Math.max(0, video.volume - 0.1);
        volumeSlider.value = video.volume;
        updateVolumeIcon();
        break;
      case 'KeyR':
        e.preventDefault();
        btnResetScaling.click();
        btnResetFilters.click();
        break;
      case 'KeyS':
        e.preventDefault();
        toggleSettingsPanel();
        break;
    }
  });

  // ==========================================================================
  // Utilities
  // ==========================================================================

  function formatTime(seconds) {
    if (isNaN(seconds)) return '00:00';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    const pad = (num) => String(num).padStart(2, '0');

    if (h > 0) {
      return `${h}:${pad(m)}:${pad(s)}`;
    }
    return `${pad(m)}:${pad(s)}`;
  }

  function showToast(message) {
    toastText.textContent = message;
    aspectToast.classList.add('show');
    setTimeout(() => {
      aspectToast.classList.remove('show');
    }, 2500);
  }
});
