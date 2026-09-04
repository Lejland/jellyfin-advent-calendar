(function () {
    const eyebrowEl = document.getElementById('calendarEyebrow');
    const titleEl = document.getElementById('calendarTitle');
    const seriesEl = document.getElementById('calendarSeries');
    const noticeEl = document.getElementById('calendarNotice');
    const noticeTitleEl = document.getElementById('calendarNoticeTitle');
    const noticeMessageEl = document.getElementById('calendarNoticeMessage');
    const messageEl = document.getElementById('calendarMessage');
    const doorsEl = document.getElementById('doors');
    const template = document.getElementById('doorTemplate');
    const overlay = document.getElementById('playerOverlay');
    const player = document.getElementById('calendarPlayer');
    const closePlayer = document.getElementById('closePlayer');
    const adventBasePath = new URL('.', document.baseURI).pathname.replace(/\/$/, '');
    const layoutConfigs = {
        desktop: {
            columns: 8,
            rows: 6,
            height: 'min(76vh, 760px)',
            maxLarge: 2,
            maxWide: 4,
            maxTall: 4
        },
        tablet: {
            columns: 6,
            rows: 7,
            height: 'min(74vh, 780px)',
            maxLarge: 1,
            maxWide: 4,
            maxTall: 3
        },
        mobile: {
            columns: 4,
            rows: 9,
            height: 'min(72vh, 860px)',
            maxLarge: 0,
            maxWide: 3,
            maxTall: 3
        }
    };
    let currentState = null;
    let resizeFrame = 0;
    let currentBackdropMetrics = null;

    function tryParseJson(value) {
        try {
            return JSON.parse(value);
        } catch (error) {
            return null;
        }
    }

    function resolveCredentials() {
        const keys = Object.keys(localStorage);
        for (const key of keys) {
            const raw = localStorage.getItem(key);
            if (!raw || !raw.includes('AccessToken')) {
                continue;
            }

            const parsed = tryParseJson(raw);
            if (!parsed) {
                continue;
            }

            if (parsed.Servers && parsed.Servers.length && parsed.Servers[0].AccessToken) {
                return parsed.Servers[0].AccessToken;
            }

            if (parsed.AccessToken) {
                return parsed.AccessToken;
            }
        }

        return '';
    }

    async function fetchJson(url, accessToken) {
        const effectiveToken = accessToken || resolveCredentials();
        const headers = {};
        if (effectiveToken) {
            headers.Authorization = 'MediaBrowser Token="' + effectiveToken + '"';
        }

        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: headers
        });

        if (response.status === 401 && !effectiveToken) {
            const fallbackToken = resolveCredentials();
            if (fallbackToken) {
                return fetchJson(url, fallbackToken);
            }
        }

        if (!response.ok) {
            throw new Error('The calendar request failed.');
        }

        const data = await response.json();
        data.__accessToken = effectiveToken;
        return data;
    }

    function valueOf(source, camelName, pascalName) {
        if (!source) {
            return undefined;
        }

        if (source[camelName] !== undefined) {
            return source[camelName];
        }

        return source[pascalName];
    }

    function normalizeDoor(door) {
        return {
            doorNumber: valueOf(door, 'doorNumber', 'DoorNumber'),
            isUnlocked: valueOf(door, 'isUnlocked', 'IsUnlocked'),
            isOpened: valueOf(door, 'isOpened', 'IsOpened'),
            isAvailable: valueOf(door, 'isAvailable', 'IsAvailable'),
            requiresResolution: valueOf(door, 'requiresResolution', 'RequiresResolution'),
            episodeId: valueOf(door, 'episodeId', 'EpisodeId') || '',
            episodeTitle: valueOf(door, 'episodeTitle', 'EpisodeTitle') || '',
            seasonNumber: valueOf(door, 'seasonNumber', 'SeasonNumber'),
            episodeNumber: valueOf(door, 'episodeNumber', 'EpisodeNumber'),
            playbackUrl: valueOf(door, 'playbackUrl', 'PlaybackUrl') || '',
            detailsUrl: valueOf(door, 'detailsUrl', 'DetailsUrl') || '',
            thumbnailUrl: valueOf(door, 'thumbnailUrl', 'ThumbnailUrl') || '',
            backdropUrl: valueOf(door, 'backdropUrl', 'BackdropUrl') || '',
            message: valueOf(door, 'message', 'Message') || ''
        };
    }

    function normalizeState(state) {
        const doors = valueOf(state, 'doors', 'Doors') || [];
        const movieModeEnabled = valueOf(state, 'movieModeEnabled', 'MovieModeEnabled') === true;
        const openedDoorCount = valueOf(state, 'openedDoorCount', 'OpenedDoorCount') || 0;
        const backgroundImageUrl = valueOf(state, 'backgroundImageUrl', 'BackgroundImageUrl') || '';
        const usesCustomBackground = backgroundImageUrl.indexOf('/adventcalendar/assets/custom-background') !== -1;

        return {
            title: valueOf(state, 'title', 'Title') || 'Advent Calendar',
            seriesTitle: valueOf(state, 'seriesTitle', 'SeriesTitle') || '',
            seasonLabel: valueOf(state, 'seasonLabel', 'SeasonLabel') || '',
            language: valueOf(state, 'language', 'Language') || '',
            isAuthenticated: valueOf(state, 'isAuthenticated', 'IsAuthenticated'),
            hasAccess: valueOf(state, 'hasAccess', 'HasAccess'),
            autoFullscreen: valueOf(state, 'autoFullscreen', 'AutoFullscreen'),
            debugUnlockAllDoors: valueOf(state, 'debugUnlockAllDoors', 'DebugUnlockAllDoors'),
            movieModeEnabled: movieModeEnabled,
            doorCount: valueOf(state, 'doorCount', 'DoorCount') || 0,
            unlockedDoorCount: valueOf(state, 'unlockedDoorCount', 'UnlockedDoorCount') || 0,
            openedDoorCount: openedDoorCount,
            backgroundImageUrl: backgroundImageUrl,
            movieModeBackdropOpened: movieModeEnabled && (openedDoorCount > 0 || usesCustomBackground),
            message: valueOf(state, 'message', 'Message') || '',
            doors: doors.map(normalizeDoor),
            __accessToken: state.__accessToken || ''
        };
    }

    function setBackground(imageUrl, accessToken) {
        const layers = [
            'linear-gradient(180deg, rgba(5, 10, 16, 0.34), rgba(5, 10, 16, 0.9))',
            'radial-gradient(circle at top, rgba(228, 158, 84, 0.18), transparent 32%)'
        ];

        if (imageUrl) {
            layers.push('url("' + withToken(imageUrl, accessToken) + '")');
        }

        document.body.style.backgroundImage = layers.join(',');
        document.body.style.backgroundPosition = 'center center, center center, center center';
        document.body.style.backgroundSize = 'cover, cover, cover';
    }

    function computeCoverMetrics(imageWidth, imageHeight, viewportWidth, viewportHeight) {
        const imageRatio = imageWidth / imageHeight;
        const viewportRatio = viewportWidth / viewportHeight;

        let renderedWidth = viewportWidth;
        let renderedHeight = viewportHeight;

        if (imageRatio > viewportRatio) {
            renderedHeight = viewportHeight;
            renderedWidth = renderedHeight * imageRatio;
        } else {
            renderedWidth = viewportWidth;
            renderedHeight = renderedWidth / imageRatio;
        }

        return {
            width: renderedWidth,
            height: renderedHeight,
            left: (viewportWidth - renderedWidth) / 2,
            top: (viewportHeight - renderedHeight) / 2
        };
    }

    function clampColor(value) {
        return Math.max(0, Math.min(255, Math.round(value)));
    }

    function applyDoorPalette(rgb) {
        if (!rgb) {
            return;
        }

        const [r, g, b] = rgb;
        const brighter = [
            clampColor((r * 0.72) + 42),
            clampColor((g * 0.64) + 28),
            clampColor((b * 0.6) + 22)
        ];
        const deeper = [
            clampColor(r * 0.42),
            clampColor(g * 0.32),
            clampColor(b * 0.28)
        ];

        document.documentElement.style.setProperty('--tile-red', 'rgba(' + brighter.join(',') + ',0.92)');
        document.documentElement.style.setProperty('--tile-red-deep', 'rgba(' + deeper.join(',') + ',0.96)');
    }

    function loadImage(url) {
        return new Promise(function (resolve, reject) {
            const image = new Image();
            image.crossOrigin = 'anonymous';
            image.onload = function () { resolve(image); };
            image.onerror = reject;
            image.src = url;
        });
    }

    async function sampleBackgroundPalette(imageUrl, accessToken) {
        if (!imageUrl) {
            currentBackdropMetrics = null;
            return;
        }

        try {
            const image = await loadImage(withToken(imageUrl, accessToken));
            const canvas = document.createElement('canvas');
            const context = canvas.getContext('2d', { willReadFrequently: true });
            const width = 32;
            const height = 32;

            canvas.width = width;
            canvas.height = height;
            context.drawImage(image, 0, 0, width, height);

            const imageData = context.getImageData(0, 0, width, height).data;
            let red = 0;
            let green = 0;
            let blue = 0;
            let total = 0;

            for (let index = 0; index < imageData.length; index += 16) {
                red += imageData[index];
                green += imageData[index + 1];
                blue += imageData[index + 2];
                total += 1;
            }

            if (total > 0) {
                applyDoorPalette([red / total, green / total, blue / total]);
            }

            currentBackdropMetrics = {
                url: withToken(imageUrl, accessToken),
                naturalWidth: image.naturalWidth || image.width,
                naturalHeight: image.naturalHeight || image.height
            };

            if (currentState && currentState.isAuthenticated && currentState.hasAccess) {
                window.requestAnimationFrame(function () {
                    renderDoors(currentState);
                });
            }
        } catch (error) {
            currentBackdropMetrics = null;
        }
    }

    function withToken(url, accessToken) {
        if (!url) {
            return '';
        }

        const separator = url.indexOf('?') === -1 ? '?' : '&';
        return accessToken ? (url + separator + 'ApiKey=' + encodeURIComponent(accessToken)) : url;
    }

    function seededNumber(seedText) {
        let hash = 2166136261;

        for (let index = 0; index < seedText.length; index += 1) {
            hash ^= seedText.charCodeAt(index);
            hash = Math.imul(hash, 16777619);
        }

        return Math.abs(hash >>> 0);
    }

    function shuffleDoors(doors, state) {
        return doors
            .slice()
            .sort(function (left, right) {
                const leftSeed = seededNumber((state.seriesTitle || state.title || 'calendar') + ':order:' + left.doorNumber);
                const rightSeed = seededNumber((state.seriesTitle || state.title || 'calendar') + ':order:' + right.doorNumber);
                return leftSeed - rightSeed;
            });
    }

    function getLayoutConfig() {
        if (window.innerWidth <= 620) {
            return layoutConfigs.mobile;
        }

        if (window.innerWidth <= 980) {
            return layoutConfigs.tablet;
        }

        return layoutConfigs.desktop;
    }

    function createOccupancy(columns, rows) {
        return Array.from({ length: rows }, function () {
            return Array(columns).fill(false);
        });
    }

    function canPlaceDoor(occupancy, columns, rows, column, row, width, height) {
        if ((column + width - 1) > columns || (row + height - 1) > rows) {
            return false;
        }

        for (let rowIndex = row - 1; rowIndex < (row + height - 1); rowIndex += 1) {
            for (let columnIndex = column - 1; columnIndex < (column + width - 1); columnIndex += 1) {
                if (occupancy[rowIndex][columnIndex]) {
                    return false;
                }
            }
        }

        return true;
    }

    function markDoorPlacement(occupancy, column, row, width, height) {
        for (let rowIndex = row - 1; rowIndex < (row + height - 1); rowIndex += 1) {
            for (let columnIndex = column - 1; columnIndex < (column + width - 1); columnIndex += 1) {
                occupancy[rowIndex][columnIndex] = true;
            }
        }
    }

    function buildCandidatePositions(columns, rows, width, height, state, door) {
        const positions = [];

        for (let row = 1; row <= (rows - height + 1); row += 1) {
            for (let column = 1; column <= (columns - width + 1); column += 1) {
                positions.push({
                    column: column,
                    row: row,
                    sortKey: seededNumber(
                        (state.seriesTitle || state.title || 'calendar') +
                        ':position:' +
                        door.doorNumber +
                        ':' +
                        width +
                        'x' +
                        height +
                        ':' +
                        column +
                        ':' +
                        row)
                });
            }
        }

        positions.sort(function (left, right) {
            return left.sortKey - right.sortKey;
        });

        return positions;
    }

    function buildShapeSequence(door, state, config, counts) {
        const seed = seededNumber((state.seriesTitle || state.title || 'calendar') + ':shape:' + door.doorNumber);
        const shapes = [];

        if (config.maxLarge > counts.large && (seed % 7 === 0)) {
            shapes.push({ width: 2, height: 2, type: 'large' });
        }

        if (config.maxWide > counts.wide && (seed % 2 === 0)) {
            shapes.push({ width: 2, height: 1, type: 'wide' });
        }

        if (config.maxTall > counts.tall && (seed % 3 === 0)) {
            shapes.push({ width: 1, height: 2, type: 'tall' });
        }

        shapes.push({ width: 1, height: 1, type: 'single' });
        return shapes;
    }

    function incrementShapeCount(counts, type) {
        if (type === 'large') {
            counts.large += 1;
        } else if (type === 'wide') {
            counts.wide += 1;
        } else if (type === 'tall') {
            counts.tall += 1;
        }
    }

    function buildDoorPlacements(doors, state, config) {
        const occupancy = createOccupancy(config.columns, config.rows);
        const placements = new Map();
        const counts = { large: 0, wide: 0, tall: 0 };

        shuffleDoors(doors, state).forEach(function (door) {
            const shapeSequence = buildShapeSequence(door, state, config, counts);
            let placement = null;

            for (const shape of shapeSequence) {
                const candidates = buildCandidatePositions(
                    config.columns,
                    config.rows,
                    shape.width,
                    shape.height,
                    state,
                    door);

                for (const candidate of candidates) {
                    if (!canPlaceDoor(
                        occupancy,
                        config.columns,
                        config.rows,
                        candidate.column,
                        candidate.row,
                        shape.width,
                        shape.height)) {
                        continue;
                    }

                    placement = {
                        column: candidate.column,
                        row: candidate.row,
                        width: shape.width,
                        height: shape.height,
                        type: shape.type
                    };
                    markDoorPlacement(occupancy, candidate.column, candidate.row, shape.width, shape.height);
                    incrementShapeCount(counts, shape.type);
                    break;
                }

                if (placement) {
                    break;
                }
            }

            if (!placement) {
                placement = {
                    column: 1,
                    row: 1,
                    width: 1,
                    height: 1,
                    type: 'single'
                };
            }

            placements.set(door.doorNumber, placement);
        });

        return placements;
    }

    function applyClosedDoorBackdrop(node, movieModeEnabled) {
        if (movieModeEnabled && currentBackdropMetrics) {
            node.style.backgroundImage = 'linear-gradient(180deg, rgba(20, 15, 12, 0.25), rgba(8, 8, 12, 0.48)),url("' + currentBackdropMetrics.url + '")';
            node.style.backgroundSize = 'cover, cover';
            node.style.backgroundPosition = 'center center, center center';
            node.style.backgroundRepeat = 'no-repeat, no-repeat';
            return;
        }
        if (!currentBackdropMetrics) {
            return;
        }

        const rect = node.getBoundingClientRect();
        const cover = computeCoverMetrics(
            currentBackdropMetrics.naturalWidth,
            currentBackdropMetrics.naturalHeight,
            window.innerWidth,
            window.innerHeight);
        const offsetX = rect.left - cover.left;
        const offsetY = rect.top - cover.top;

        node.style.backgroundImage =
            'linear-gradient(180deg, rgba(32, 20, 18, 0.4), rgba(18, 12, 12, 0.62)),url("' +
            currentBackdropMetrics.url +
            '")';
        node.style.backgroundSize = 'auto, ' + cover.width + 'px ' + cover.height + 'px';
        node.style.backgroundPosition = 'center center, ' + (-offsetX) + 'px ' + (-offsetY) + 'px';
        node.style.backgroundRepeat = 'no-repeat, no-repeat';
    }

    function setStatusMessage(message) {
        if (!message) {
            messageEl.textContent = '';
            messageEl.classList.add('is-hidden');
            return;
        }

        messageEl.textContent = message;
        messageEl.classList.remove('is-hidden');
    }

    function setNotice(title, message) {
        noticeTitleEl.textContent = title;
        noticeMessageEl.textContent = message;
        noticeEl.classList.remove('is-hidden');
    }

    function clearNotice() {
        noticeEl.classList.add('is-hidden');
    }

    function buildClosedMeta(door) {
        if (!door.isAvailable && door.message && door.isUnlocked) {
            return door.message;
        }

        if (door.isUnlocked && door.isAvailable) {
            return 'Open today';
        }

        return '';
    }

    function buildOpenedMeta(door) {
        if (door.seasonNumber && door.episodeNumber) {
            return 'S' + door.seasonNumber + 'E' + door.episodeNumber;
        }

        return door.message || 'Opened';
    }

    function applyOpenedState(node, door, accessToken) {
        node.classList.remove('is-closed');
        node.classList.add('is-opened');
        node.querySelector('.door__day').textContent = '';
        node.querySelector('.door__opened-title').textContent = door.episodeTitle || ('Door ' + door.doorNumber);
        node.querySelector('.door__opened-meta').textContent = buildOpenedMeta(door);
        node.querySelector('.door__meta').textContent = '';

        if (door.thumbnailUrl) {
            node.style.backgroundImage =
                'linear-gradient(180deg, rgba(8, 12, 18, 0.12), rgba(8, 12, 18, 0.56)),url("' +
                withToken(door.thumbnailUrl, accessToken) +
                '")';
            node.style.backgroundSize = '';
            node.style.backgroundPosition = '';
            node.style.backgroundRepeat = '';
        }
    }

    function renderDoors(state) {
        doorsEl.innerHTML = '';
        clearNotice();

        if (!state.isAuthenticated) {
            doorsEl.classList.add('is-hidden');
            setNotice('Sign in required', state.message || 'Sign in to Jellyfin to open the advent calendar. Happy holidays.');
            setStatusMessage('');
            return;
        }

        if (!state.hasAccess) {
            doorsEl.classList.add('is-hidden');
            setNotice('Access denied', state.message || 'This Jellyfin user does not have access to the advent calendar.');
            setStatusMessage('');
            return;
        }

        doorsEl.classList.remove('is-hidden');
        const config = getLayoutConfig();
        const placements = buildDoorPlacements(state.doors, state, config);
        const closedDoorNodes = [];

        doorsEl.style.setProperty('--door-columns', String(config.columns));
        doorsEl.style.setProperty('--door-rows', String(config.rows));
        doorsEl.style.setProperty('--door-height', config.height);

        shuffleDoors(state.doors, state).forEach(function (door) {
            const node = template.content.firstElementChild.cloneNode(true);
            const dayEl = node.querySelector('.door__day');
            const metaEl = node.querySelector('.door__meta');
            const openedTitleEl = node.querySelector('.door__opened-title');
            const openedMetaEl = node.querySelector('.door__opened-meta');
            const placement = placements.get(door.doorNumber);

            dayEl.textContent = door.doorNumber;
            metaEl.textContent = buildClosedMeta(door);
            openedTitleEl.textContent = door.episodeTitle || ('Door ' + door.doorNumber);
            openedMetaEl.textContent = buildOpenedMeta(door);
            node.style.gridColumn = String(placement.column) + ' / span ' + String(placement.width);
            node.style.gridRow = String(placement.row) + ' / span ' + String(placement.height);
            node.classList.add('is-closed');

            if (!door.isUnlocked) {
                node.classList.add('is-locked');
            }

            if (!door.isAvailable) {
                node.classList.add('is-missing');
            }

            if (!door.isAvailable && !door.message) {
                node.classList.add('is-hidden');
            }

            if (door.isOpened && door.thumbnailUrl) {
                node.classList.remove('is-closed');
                node.classList.add('is-opened');
                node.style.backgroundImage =
                    'linear-gradient(180deg, rgba(8, 12, 18, 0.12), rgba(8, 12, 18, 0.56)),url("' +
                    withToken(door.thumbnailUrl, state.__accessToken) +
                    '")';
                node.style.backgroundSize = '';
                node.style.backgroundPosition = '';
                node.style.backgroundRepeat = '';
                metaEl.textContent = '';
            } else {
                closedDoorNodes.push(node);
            }

            node.addEventListener('click', async function () {
                if (!door.isUnlocked || !door.isAvailable) {
                    return;
                }

                const originalMeta = metaEl.textContent;
                node.disabled = true;
                metaEl.textContent = 'Opening...';

                try {
                    const resolvedDoor = normalizeDoor(await fetchJson(adventBasePath + '/door/' + door.doorNumber, state.__accessToken));
                    if (!resolvedDoor.isAvailable) {
                        node.classList.add('is-missing');
                        metaEl.textContent = resolvedDoor.message || 'No episode is available for this door.';
                        return;
                    }

                    applyOpenedState(node, resolvedDoor, state.__accessToken);
                    if (state.movieModeEnabled && (state.backgroundImageUrl || '').indexOf('/adventcalendar/assets/custom-background') === -1) {
                        const stateDoor = state.doors.find(function (item) { return item.doorNumber === resolvedDoor.doorNumber; });
                        if (stateDoor) { Object.assign(stateDoor, resolvedDoor); }
                        const refreshedState = normalizeState(await fetchJson(adventBasePath + '/state', state.__accessToken));
                        state.movieModeBackdropOpened = refreshedState.movieModeBackdropOpened;
                        state.backgroundImageUrl = refreshedState.backgroundImageUrl;
                        setBackground(state.backgroundImageUrl, state.__accessToken);
                        sampleBackgroundPalette(state.backgroundImageUrl, state.__accessToken);
                    }
                    await playDoor(state, resolvedDoor);
                } catch (error) {
                    metaEl.textContent = error.message || originalMeta;
                } finally {
                    node.disabled = false;
                }
            });

            doorsEl.appendChild(node);
        });

        window.requestAnimationFrame(function () {
            closedDoorNodes.forEach(function (node) {
                applyClosedDoorBackdrop(node, state.movieModeEnabled && !state.movieModeBackdropOpened);
            });
        });
    }

    async function playDoor(state, door) {
        overlay.classList.remove('is-hidden');
        player.src = door.playbackUrl + (state.__accessToken ? '&ApiKey=' + encodeURIComponent(state.__accessToken) : '');

        try {
            await player.play();
            if (state.autoFullscreen && player.requestFullscreen) {
                await player.requestFullscreen();
            }
        } catch (error) {
            if (door.detailsUrl) {
                window.location.assign(door.detailsUrl);
                return;
            }

            throw error;
        }
    }

    function closePlayback() {
        player.pause();
        player.removeAttribute('src');
        player.load();
        overlay.classList.add('is-hidden');

        if (document.fullscreenElement) {
            document.exitFullscreen().catch(function () {});
        }
    }

    closePlayer.addEventListener('click', closePlayback);
    overlay.addEventListener('click', function (event) {
        if (event.target === overlay) {
            closePlayback();
        }
    });

    window.addEventListener('resize', function () {
        if (!currentState || !currentState.isAuthenticated || !currentState.hasAccess) {
            return;
        }

        window.cancelAnimationFrame(resizeFrame);
        resizeFrame = window.requestAnimationFrame(function () {
            renderDoors(currentState);
        });
    });

    fetchJson(adventBasePath + '/state')
        .then(function (rawState) {
            currentState = normalizeState(rawState);
            eyebrowEl.textContent = currentState.seasonLabel || 'Advent Calendar';
            titleEl.textContent = currentState.title || 'Advent Calendar';
            seriesEl.textContent = currentState.seriesTitle || '';
            setBackground(currentState.backgroundImageUrl, currentState.__accessToken);
            sampleBackgroundPalette(currentState.backgroundImageUrl, currentState.__accessToken);
            setStatusMessage(currentState.isAuthenticated && currentState.hasAccess ? currentState.message : '');
            renderDoors(currentState);
        })
        .catch(function (error) {
            titleEl.textContent = 'Advent Calendar';
            seriesEl.textContent = '';
            setNotice('Calendar unavailable', error.message || 'The calendar could not be loaded.');
        });
})();
