(function ($) {
  var statusOptions = ['Shipped', 'Pending', 'Processing', 'Cancelled'];

  var rows = [
    { id: 4001, customer: 'Acme', product: 'Sensor A', qty: 4, status: 'Shipped', rush: false },
    { id: 4002, customer: 'Globex', product: 'Cable kit', qty: 12, status: 'Pending', rush: true },
    { id: 4003, customer: 'Initech', product: 'Bracket', qty: 50, status: 'Shipped', rush: false },
    { id: 4004, customer: 'Umbrella', product: 'Mount plate', qty: 7, status: 'Cancelled', rush: false },
    { id: 4005, customer: 'Stark', product: 'Power module', qty: 2, status: 'Processing', rush: true },
  ];

  var treeData = [
    { label: 'North America', open: true, children: ['USA', 'Canada', 'Mexico'] },
    { label: 'Europe', open: false, children: ['DE', 'FR', 'UK'] },
    { label: 'Asia', open: false, children: ['CN', 'JP', 'KR'] },
  ];

  var editing = null;

  var divGridRows = [
    { id: 1, code: 'N-01', name: 'Nail box', units: 50, ok: true },
    { id: 2, code: 'N-02', name: 'Screw M4', units: 200, ok: false },
    { id: 3, code: 'N-03', name: 'Bracket', units: 8, ok: true },
  ];
  var divEditing = null;
  var pqHost$ = null;
  var pqData = [
    { sku: 'A-10', label: 'Gear', qty: 6, bin: 'P1' },
    { sku: 'A-11', label: 'Shaft', qty: 14, bin: 'P2' },
    { sku: 'A-12', label: 'Bearing', qty: 30, bin: 'P3' },
  ];

  function destroyPq() {
    if (pqHost$) {
      try {
        pqHost$.pqGrid('destroy');
      } catch (e) {
        /* ignore */
      }
      pqHost$ = null;
    }
  }

  function initPq() {
    destroyPq();
    var $host = $('#pq-grid-host');
    if (!$host.length || !$.fn.pqGrid) return;
    var w = Math.max(320, $host.parent().width() || 640);
    pqHost$ = $host.pqGrid({
      width: w,
      height: 260,
      editable: true,
      dataModel: { location: 'local', data: pqData },
      colModel: [
        { title: 'SKU', dataIndx: 'sku', width: 90, editable: true },
        { title: 'Label', dataIndx: 'label', width: 200, editable: true },
        { title: 'Qty', dataIndx: 'qty', width: 70, dataType: 'integer', editable: true },
        { title: 'Bin', dataIndx: 'bin', width: 70, editable: true },
      ],
    });
  }

  function findDivRow(id) {
    for (var j = 0; j < divGridRows.length; j++) {
      if (divGridRows[j].id === id) return divGridRows[j];
    }
    return null;
  }

  function commitDivEdit(r, field, rawVal) {
    if (field === 'units') {
      var nu = parseInt(rawVal, 10);
      r.units = isNaN(nu) || nu < 0 ? 0 : nu;
    } else {
      r[field] = rawVal;
    }
    divEditing = null;
    renderDivGrid();
  }

  function renderDivGrid() {
    var $body = $('#div-grid-body').empty();
    divGridRows.forEach(function (r) {
      var $row = $('<div class="faux-row" role="row">');
      $row.append($('<div class="faux-cell" role="gridcell">').text(r.code));

      var $nameCell = $('<div class="faux-cell faux-edit" role="gridcell">');
      if (divEditing && divEditing.id === r.id && divEditing.field === 'name') {
        var $nIn = $('<input type="text" class="cell-input">').val(String(r.name));
        $nIn.on('blur', function () {
          commitDivEdit(r, 'name', $(this).val());
        });
        $nIn.on('keydown', function (e) {
          if (e.key === 'Enter') {
            e.preventDefault();
            $(this).blur();
          }
        });
        $nameCell.append($nIn);
        setTimeout(function () {
          $nIn.trigger('focus');
          if ($nIn[0] && $nIn[0].select) $nIn[0].select();
        }, 0);
      } else {
        $nameCell.append(
          $('<span class="cell-display">')
            .attr('data-row-id', r.id)
            .attr('data-field', 'name')
            .text(String(r.name))
        );
      }

      var $unitsCell = $('<div class="faux-cell faux-edit" role="gridcell">');
      if (divEditing && divEditing.id === r.id && divEditing.field === 'units') {
        var $uIn = $('<input type="number" min="0" class="cell-input cell-input-narrow">').val(r.units);
        $uIn.on('blur', function () {
          commitDivEdit(r, 'units', $(this).val());
        });
        $uIn.on('keydown', function (e) {
          if (e.key === 'Enter') {
            e.preventDefault();
            $(this).blur();
          }
        });
        $unitsCell.append($uIn);
        setTimeout(function () {
          $uIn.trigger('focus');
          if ($uIn[0] && $uIn[0].select) $uIn[0].select();
        }, 0);
      } else {
        $unitsCell.append(
          $('<span class="cell-display">')
            .attr('data-row-id', r.id)
            .attr('data-field', 'units')
            .text(String(r.units))
        );
      }

      var $ok = $('<input type="checkbox">')
        .prop('checked', !!r.ok)
        .attr('aria-label', 'OK ' + r.code)
        .on('change', function () {
          r.ok = $(this).prop('checked');
        });
      $row.append($nameCell, $unitsCell, $('<div class="faux-cell faux-center" role="gridcell">').append($ok));
      $body.append($row);
    });
  }

  function cellId(id, field) {
    return 'cell-' + id + '-' + field;
  }

  function findRow(id) {
    for (var i = 0; i < rows.length; i++) {
      if (rows[i].id === id) return rows[i];
    }
    return null;
  }

  function mkEditableCell(r, field, inputType) {
    var $td = $('<td>').addClass('cell-edit');
    var active = editing && editing.id === r.id && editing.field === field;
    if (active) {
      var $inp = $('<input>')
        .addClass('cell-input')
        .attr('id', cellId(r.id, field))
        .attr('type', inputType || 'text');
      if (inputType === 'number') {
        $inp.attr('min', 0).addClass('cell-input-narrow');
        $inp.val(r[field]);
      } else {
        $inp.val(String(r[field]));
      }
      $inp.on('blur', function () {
        commitEdit(r, field, $inp.val());
      });
      $inp.on('keydown', function (e) {
        if (e.key === 'Enter') {
          e.preventDefault();
          $(this).blur();
        }
      });
      $td.append($inp);
      setTimeout(function () {
        $inp.trigger('focus');
        if ($inp[0] && $inp[0].select) $inp[0].select();
      }, 0);
    } else {
      var $span = $('<span>')
        .addClass('cell-display')
        .attr('data-row-id', r.id)
        .attr('data-field', field)
        .text(String(r[field]));
      $td.append($span);
    }
    return $td;
  }

  function commitEdit(r, field, rawVal) {
    if (field === 'qty') {
      var n = parseInt(rawVal, 10);
      r.qty = isNaN(n) || n < 0 ? 0 : n;
    } else {
      r[field] = rawVal;
    }
    editing = null;
    renderTable();
  }

  function renderTable() {
    var $tb = $('#table-body').empty();
    rows.forEach(function (r) {
      var $tr = $('<tr>');
      $tr.append($('<td>').text(r.id));
      $tr.append(mkEditableCell(r, 'customer', 'text'));
      $tr.append(mkEditableCell(r, 'product', 'text'));
      $tr.append(mkEditableCell(r, 'qty', 'number'));

      var $selTd = $('<td>');
      var $sel = $('<select>').addClass('cell-select').attr('aria-label', 'Status for order ' + r.id);
      statusOptions.forEach(function (s) {
        $sel.append($('<option>').val(s).text(s));
      });
      $sel.val(r.status);
      $sel.on('change', function () {
        r.status = $(this).val();
      });
      $selTd.append($sel);
      $tr.append($selTd);

      var $rush = $('<input type="checkbox">').prop('checked', !!r.rush).attr('aria-label', 'Rush order ' + r.id);
      $rush.on('change', function () {
        r.rush = $(this).prop('checked');
      });
      $tr.append($('<td>').addClass('cell-center').append($rush));

      $tb.append($tr);
    });
  }

  function renderTree() {
    var $root = $('#tree-root').empty();
    treeData.forEach(function (n, idx) {
      var $li = $('<li class="tree-node" role="treeitem">').attr('aria-expanded', n.open);
      var $btn = $('<button type="button" class="twisty">').text(n.open ? '▼' : '▶');
      $btn.on('click', function () {
        n.open = !n.open;
        renderTree();
      });
      $li.append($btn, document.createTextNode(' ' + n.label));
      var $sub = $('<ul role="group">').toggle(n.open);
      n.children.forEach(function (c) {
        $sub.append($('<li role="treeitem">').text(c));
      });
      $li.append($sub);
      $root.append($li);
    });
  }

  $(function () {
    renderTable();
    renderTree();
    renderDivGrid();

    $('#table-body').on('click', '.cell-display', function () {
      var id = Number($(this).data('row-id'), 10);
      var field = $(this).data('field');
      if (!field) return;
      editing = { id: id, field: field };
      renderTable();
    });

    $('#div-grid-body').on('click', '.cell-display', function () {
      var id = Number($(this).data('row-id'), 10);
      var field = $(this).data('field');
      if (!field || !findDivRow(id)) return;
      divEditing = { id: id, field: field };
      renderDivGrid();
    });

    var currentTab = 0;
    $('.menu-item').each(function () {
      var $item = $(this);
      var $btn = $item.find('.menu-btn');
      var $pop = $item.find('.popup');
      $item.on('mouseenter', function () {
        $pop.prop('hidden', false);
        $btn.attr('aria-expanded', 'true');
      });
      $item.on('mouseleave', function () {
        $pop.prop('hidden', true);
        $btn.attr('aria-expanded', 'false');
      });
      $pop.on('click', 'button', function () {
        $pop.prop('hidden', true);
        $btn.attr('aria-expanded', 'false');
      });
    });

    $('.tabs [role="tab"]').on('click', function () {
      var i = Number($(this).data('tab'), 10);
      if (currentTab === 3 && i !== 3) destroyPq();
      currentTab = i;
      $('.tabs [role="tab"]').removeClass('on').attr('aria-selected', 'false');
      $(this).addClass('on').attr('aria-selected', 'true');
      $('[data-panel]').each(function () {
        var show = Number($(this).data('panel'), 10) === i;
        $(this).prop('hidden', !show);
      });
      if (i === 3) setTimeout(initPq, 0);
    });

    $('#notes-area, #notes-sync').on('input', function () {
      var v = $(this).val();
      $('#notes-area').val(v);
      $('#notes-sync').val(v);
    });

    $('#main-form').on('submit', function (e) {
      e.preventDefault();
    });

    $('#btn-native-dlg').on('click', function () {
      document.getElementById('nativeDlg').showModal();
    });
    $('#btn-close-native').on('click', function () {
      document.getElementById('nativeDlg').close();
    });

    $('#btn-pseudo-dlg').on('click', function () {
      $('#pseudo-overlay').prop('hidden', false);
    });
    $('#btn-close-pseudo').on('click', function () {
      $('#pseudo-overlay').prop('hidden', true);
    });
    $('#pseudo-overlay').on('click', function (e) {
      if (e.target === this) $('#pseudo-overlay').prop('hidden', true);
    });
    $('.pseudo-box').on('click', function (e) {
      e.stopPropagation();
    });

    $('#btn-open-pw-sim').on('click', function () {
      var usePseudo = $('#pw-sim-toggle').prop('checked');
      if (usePseudo) {
        $('#pw-sim-overlay').prop('hidden', false);
      } else {
        document.getElementById('pwSimNativeDlg').showModal();
      }
    });
    $('#btn-close-pw-native').on('click', function () {
      document.getElementById('pwSimNativeDlg').close();
    });
    $('#btn-close-pw-pseudo').on('click', function () {
      $('#pw-sim-overlay').prop('hidden', true);
    });
    $('#pw-sim-overlay').on('click', function (e) {
      if (e.target === this) $('#pw-sim-overlay').prop('hidden', true);
    });

    $('#btn-open-mirror-window').on('click', function () {
      window.open('./mirror-window.html', 'mars_mirror_window', 'width=920,height=700,resizable=yes,scrollbars=yes');
    });

    var $fakeBtn = $('#fake-select-btn');
    var $fakeList = $('#fake-select-list');
    $fakeBtn.on('click', function () {
      var open = $fakeList.prop('hidden');
      $fakeList.prop('hidden', !open);
      $fakeBtn.attr('aria-expanded', open ? 'true' : 'false');
    });
    $fakeList.on('click', '[role="option"]', function () {
      $fakeBtn.text($(this).text());
      $fakeList.prop('hidden', true);
      $fakeBtn.attr('aria-expanded', 'false');
    });
    $(document).on('click', function (e) {
      if (!$(e.target).closest('.fake-select').length) {
        $fakeList.prop('hidden', true);
        $fakeBtn.attr('aria-expanded', 'false');
      }
    });
  });
})(jQuery);
