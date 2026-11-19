(() => {
  'use strict';
  const colors = ['#235ba3', '#08735f', '#ad4a25', '#72439b', '#a9324f', '#304057'];
  const ink = '#172e3e', muted = '#4c6270', grid = '#dce9e5', axis = '#849d9a';
  const number = new Intl.NumberFormat('tr-TR', {maximumFractionDigits: 5, useGrouping: false});
  let active;

  function range(min, max, divisions, pad = true) {
    if (min === max) {
      const spread = min === 0 ? 1 : Math.abs(min) * .1;
      min -= spread;
      max += spread;
    }
    const raw = (max - min) / divisions;
    const tens = 10 ** Math.floor(Math.log10(raw));
    const step = [1, 2, 2.5, 5, 10].find(factor => factor * tens >= raw) * tens;
    const margin = pad ? (max - min) * .045 : 0;
    const lo = Math.floor((min - margin) / step) * step;
    const hi = Math.ceil((max + margin) / step) * step;
    const ticks = Array.from({length: Math.round((hi - lo) / step) + 1}, (_, index) => {
      const tick = lo + index * step;
      return Math.abs(tick) < step * 1e-8 ? 0 : tick;
    });
    return {lo, hi, ticks};
  }

  function tick(value) {
    const magnitude = Math.abs(value);
    return magnitude && (magnitude >= 1e4 || magnitude < .001) ? value.toExponential(1).replace('.', ',') : number.format(value);
  }
  function line(ctx, x1, y1, x2, y2, color, width = 1) {
    ctx.strokeStyle = color;
    ctx.lineWidth = width;
    ctx.beginPath();
    ctx.moveTo(x1, y1); ctx.lineTo(x2, y2);
    ctx.stroke();
  }
  function marker(ctx, x, y, color, index) {
    ctx.fillStyle = 'white';
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.7;
    ctx.beginPath();
    if (index % 3 === 0) ctx.arc(x, y, 4, 0, 2 * Math.PI);
    else if (index % 3 === 1) ctx.rect(x - 3.5, y - 3.5, 7, 7);
    else {
      ctx.moveTo(x, y - 4.5); ctx.lineTo(x + 4.5, y); ctx.lineTo(x, y + 4.5); ctx.lineTo(x - 4.5, y); ctx.closePath();
    }
    ctx.fill();
    ctx.stroke();
  }
  function text(ctx, value, x, y, size, color = ink, align = 'left') {
    ctx.font = size + 'px system-ui, sans-serif';
    ctx.fillStyle = color;
    ctx.textAlign = align;
    ctx.fillText(value, x, y);
  }
  function legendGroups(ctx, traces, width) {
    ctx.font = '11px system-ui, sans-serif';
    const rows = [[]];
    let used = 0;
    traces.forEach((trace, index) => {
      const length = ctx.measureText(trace.label).width + 33;
      if (used + length > width - 20 && rows.at(-1).length) { rows.push([]); used = 0; }
      rows.at(-1).push({index, length});
      used += length;
    });
    return rows;
  }
  function render(canvas, plot, width, height, scale, exporting) {
    canvas.width = Math.round(width * scale);
    canvas.height = Math.round(height * scale);
    const ctx = canvas.getContext('2d');
    if (!ctx || width < 170) return;
    ctx.scale(scale, scale);
    ctx.fillStyle = 'white';
    ctx.fillRect(0, 0, width, height);
    if (exporting) text(ctx, plot.title, 20, 29, 21);
    const groups = legendGroups(ctx, plot.traces, width);
    const legendTop = height - (exporting ? 54 : 10) - groups.length * 18;
    const points = plot.traces.flatMap(trace => trace.points);
    let x = range(Math.min(...points.map(p => p[0])), Math.max(...points.map(p => p[0])), width > 500 ? 6 : 4);
    let y = range(Math.min(...points.map(p => p[1])), Math.max(...points.map(p => p[1])), 5);
    let left = 58, right = width - 17, top = exporting ? 55 : 19, bottom = legendTop - 53;
    if (plot.equalAxes) {
      const limit = Math.max(Math.abs(x.lo), Math.abs(x.hi), Math.abs(y.lo), Math.abs(y.hi));
      x = y = range(-limit, limit, 5, false);
      const side = Math.min(right - left, bottom - top);
      left += (right - left - side) / 2;
      right = left + side;
      bottom = top + side;
    }
    const px = value => left + (value - x.lo) / (x.hi - x.lo) * (right - left);
    const py = value => bottom - (value - y.lo) / (y.hi - y.lo) * (bottom - top);
    x.ticks.forEach(value => {
      const a = px(value);
      line(ctx, a, top, a, bottom, value ? grid : axis);
      text(ctx, tick(value), a, bottom + 15, 10, muted, 'center');
    });
    y.ticks.forEach(value => {
      const a = py(value);
      line(ctx, left, a, right, a, value ? grid : axis);
      text(ctx, tick(value), left - 6, a + 3.5, 10, muted, 'right');
    });
    line(ctx, left, top, left, bottom, axis);
    line(ctx, left, bottom, right, bottom, axis);
    text(ctx, plot.x, (left + right) / 2, bottom + 35, 12, ink, 'center');
    ctx.save();
    ctx.translate(12, (top + bottom) / 2);
    ctx.rotate(-Math.PI / 2);
    text(ctx, plot.y, 0, 0, 12, ink, 'center');
    ctx.restore();
    plot.traces.forEach((trace, index) => {
      const color = colors[index % colors.length];
      const mapped = trace.points.map(([a, b]) => [px(a), py(b)]);
      if (trace.style === 'Points' || mapped.length === 1) {
        mapped.forEach(([a, b]) => marker(ctx, a, b, color, index));
        return;
      }
      ctx.setLineDash(trace.style === 'Guide' ? [6, 4] : []);
      ctx.strokeStyle = color;
      ctx.lineWidth = 1.9;
      ctx.beginPath();
      ctx.moveTo(...mapped[0]);
      mapped.slice(1).forEach(point => ctx.lineTo(...point));
      ctx.stroke();
      ctx.setLineDash([]);
      if (trace.style === 'Vector') {
        const [ox, oy] = mapped[0], [ex, ey] = mapped.at(-1);
        if (ox === ex && oy === ey) marker(ctx, ex, ey, color, index);
        else {
          const angle = Math.atan2(ey - oy, ex - ox);
          for (const direction of [angle - Math.PI / 6, angle + Math.PI / 6]) {
            line(ctx, ex, ey, ex - 8 * Math.cos(direction), ey - 8 * Math.sin(direction), color, 1.9);
          }
        }
      }
    });
    groups.forEach((group, row) => {
      let position = 10;
      const baseline = legendTop + row * 18 + 8;
      group.forEach(({index, length}) => {
        const trace = plot.traces[index], color = colors[index % colors.length];
        if (trace.style === 'Points') marker(ctx, position + 7, baseline, color, index);
        else {
          ctx.setLineDash(trace.style === 'Guide' ? [4, 3] : []);
          line(ctx, position, baseline, position + 15, baseline, color, 1.8);
          ctx.setLineDash([]);
        }
        text(ctx, trace.label, position + 20, baseline + 4, 11);
        position += length;
      });
    });
    if (exporting && plot.caption) {
      ctx.font = '13px system-ui, sans-serif';
      let nextLine = '', baseline = height - 32;
      for (const word of plot.caption.split(' ')) {
        const next = nextLine ? nextLine + ' ' + word : word;
        if (ctx.measureText(next).width > width - 40 && nextLine) {
          text(ctx, nextLine, 20, baseline, 13, muted);
          baseline += 16;
          nextLine = word;
        } else nextLine = next;
      }
      text(ctx, nextLine, 20, baseline, 13, muted);
    }
  }
  function refresh() {
    if (!active?.canvas.isConnected) { active = null; return; }
    const {canvas, plot} = active;
    render(canvas, plot, canvas.clientWidth, canvas.clientHeight, Math.min(window.devicePixelRatio || 1, 3), false);
  }
  function save(plot) {
    const canvas = document.createElement('canvas');
    render(canvas, plot, 720, 540, 2, true);
    const link = document.createElement('a');
    link.href = canvas.toDataURL('image/png');
    link.download = 'FizikLab-grafik.png';
    document.body.append(link);
    link.click();
    link.remove();
  }
  window.addEventListener('resize', refresh);
  window.fizikLab = {
    read: key => localStorage.getItem('fiziklab/v1/' + key),
    write: (key, value) => localStorage.setItem('fiziklab/v1/' + key, value),
    top: () => window.scrollTo(0, 0),
    draw: (canvas, plot) => {
      active = {canvas, plot};
      refresh();
      document.getElementById('save-chart').onclick = () => save(plot);
    }
  };
})();
