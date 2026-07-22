import 'dart:async';
import 'package:flutter/material.dart';
import 'package:borrador_dev_app/screen/detalle_screen.dart';
import 'package:borrador_dev_app/services/borrador_dev_api.dart';

class BorradorFormScreen extends StatefulWidget {
  final BorradorDevApi api;
  final String baseUrl;
  final Future<void> Function() onOpenParametros;

  const BorradorFormScreen({
    super.key,
    required this.api,
    required this.baseUrl,
    required this.onOpenParametros,
  });

  @override
  State<BorradorFormScreen> createState() => _BorradorFormScreenState();
}

class _BorradorFormScreenState extends State<BorradorFormScreen> {
  final _formKey = GlobalKey<FormState>();

  static const String usuarioFijo = "admi";

  final numeroCtrl = TextEditingController(); // solo lectura
  final fechaCtrl = TextEditingController(); // yyyy-MM-dd
  final observacionCtrl = TextEditingController();

  final codigoCtrl = TextEditingController();
  final numeroCajaCtrl = TextEditingController(text: "1"); // default 1 y persistente

  final codigoFocus = FocusNode();

  bool loading = false;
  String? error;
  bool _validarDetalle = false;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    fechaCtrl.text =
        "${now.year.toString().padLeft(4, '0')}-${now.month.toString().padLeft(2, '0')}-${now.day.toString().padLeft(2, '0')}";

    Future.delayed(const Duration(milliseconds: 250), () {
      if (mounted) codigoFocus.requestFocus();
    });
  }

  @override
  void dispose() {
    numeroCtrl.dispose();
    fechaCtrl.dispose();
    observacionCtrl.dispose();
    codigoCtrl.dispose();
    numeroCajaCtrl.dispose();
    codigoFocus.dispose();
    super.dispose();
  }

  InputDecoration _dec(String label, {Widget? suffix}) => InputDecoration(
        labelText: label,
        border: const OutlineInputBorder(),
        isDense: true,
        contentPadding: const EdgeInsets.symmetric(horizontal: 10, vertical: 10),
        suffixIcon: suffix,
      );

  Future<void> _pickFecha() async {
    DateTime current = DateTime.now();
    final parts = fechaCtrl.text.trim().split('-');
    if (parts.length == 3) {
      final y = int.tryParse(parts[0]);
      final m = int.tryParse(parts[1]);
      final d = int.tryParse(parts[2]);
      if (y != null && m != null && d != null) current = DateTime(y, m, d);
    }

    final picked = await showDatePicker(
      context: context,
      initialDate: current,
      firstDate: DateTime(2020, 1, 1),
      lastDate: DateTime(2100, 12, 31),
    );

    if (picked != null) {
      fechaCtrl.text =
          "${picked.year.toString().padLeft(4, '0')}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}";
    }
  }

  String? _reqInt(String? v) {
    if (!_validarDetalle) return null;
    if (v == null || v.trim().isEmpty) return 'Req';
    if (int.tryParse(v.trim()) == null) return 'Inv';
    return null;
  }

  int _cajaOrDefault() {
    final v = int.tryParse(numeroCajaCtrl.text.trim());
    return (v == null || v <= 0) ? 1 : v;
  }

  String _prettyErr(Object e) {
    // Si es tu excepción bonita, ya trae el mensaje limpio.
    if (e is BorradorDevApiException) return e.message;
    return e.toString();
  }

  void _nuevo() {
    setState(() {
      error = null;
      _validarDetalle = false;
      numeroCtrl.clear();
      observacionCtrl.clear();
      codigoCtrl.clear();

      if (numeroCajaCtrl.text.trim().isEmpty) numeroCajaCtrl.text = "1";
    });

    codigoFocus.requestFocus();

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text("Nuevo borrador listo. Escanee y guarde para crear el número.")),
    );
  }

  Future<void> _buscarNumeroSheet() async {
    if (loading) return;

    final selected = await showModalBottomSheet<int?>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _BuscarNumeroSheet(api: widget.api),
    );

    if (selected != null) {
      setState(() {
        numeroCtrl.text = selected.toString();
        error = null;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text("Número seleccionado: $selected")),
      );

      Future.delayed(const Duration(milliseconds: 150), () {
        if (mounted) codigoFocus.requestFocus();
      });
    }
  }

  Future<void> _guardar() async {
    setState(() {
      loading = true;
      error = null;
      _validarDetalle = true;
    });

    try {
      if (!_formKey.currentState!.validate()) {
        setState(() => loading = false);
        return;
      }

      final fechaYmd = fechaCtrl.text.trim().isEmpty ? null : fechaCtrl.text.trim();
      final obs = observacionCtrl.text.trim();

      final codigo = codigoCtrl.text.trim();
      if (codigo.isEmpty) {
        setState(() => error = "CodigoBarra es requerido.");
        return;
      }

      final caja = _cajaOrDefault();
      final numeroActual = int.tryParse(numeroCtrl.text.trim());

      // Si NO hay número -> create() crea número y mete primera línea
      if (numeroActual == null) {
        final nuevoNumero = await widget.api.create(
          usuario: usuarioFijo,
          observacion: obs,
          fechaYmd: fechaYmd,
          codigoBarra: codigo,
          numeroCaja: caja,
          cantidad: 1, // primer escaneo
          status: 0,
        );

        numeroCtrl.text = nuevoNumero.toString();

        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text("Creado #$nuevoNumero. Línea agregada (Caja $caja).")),
        );
      } else {
        // Si YA hay número -> scan() autoincrementa cantidad si se repite código en misma caja
        final res = await widget.api.scan(
          numero: numeroActual,
          codigoBarra: codigo,
          numeroCaja: caja,
          cantidad: 1,
        );

        final updated = (res["updated"] is num) ? (res["updated"] as num).toInt() : 0;
        final inserted = (res["inserted"] is num) ? (res["inserted"] as num).toInt() : 0;

        if (!mounted) return;

        if (updated > 0) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text("Cantidad incrementada en #$numeroActual (Caja $caja).")),
          );
        } else if (inserted > 0) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text("Línea agregada en #$numeroActual (Caja $caja).")),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text("Sin cambios en #$numeroActual.")),
          );
        }
      }

      // limpia SOLO el código para siguiente escaneo
      codigoCtrl.clear();
      codigoFocus.requestFocus();
    } catch (e) {
      final msg = _prettyErr(e);
      setState(() => error = msg);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg)),
        );
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _aprobar() async {
    setState(() {
      loading = true;
      error = null;
      _validarDetalle = false;
    });

    try {
      final numero = int.tryParse(numeroCtrl.text.trim());
      if (numero == null) {
        setState(() => error = "Primero crea un número (Nuevo + Guardar) o usa Buscar.");
        return;
      }

      final res = await widget.api.run(numero: numero, aprobar: true);

      final info = (res["info"] as Map?)?.cast<String, dynamic>();
      final procesados = info?["NumerosProcesados"];
      final int nProcesados =
          (procesados is int) ? procesados : (procesados is num ? procesados.toInt() : 0);

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            nProcesados > 0 ? "Aprobado con éxito (#$numero)" : "No se encontró el número o no hay registros",
          ),
        ),
      );
    } catch (e) {
      final msg = _prettyErr(e);
      setState(() => error = msg);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  void _abrirDetalle() {
    final n = int.tryParse(numeroCtrl.text.trim());
    if (n == null) {
      setState(() => error = "Primero crea un número o usa Buscar.");
      return;
    }

    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => DetalleScreen(api: widget.api, numero: n),
      ),
    );
  }

  Widget _sectionTitle(String text) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Text(text, style: const TextStyle(fontWeight: FontWeight.w700)),
      );

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final isWide = width >= 700;

    return Scaffold(
      resizeToAvoidBottomInset: true,
      appBar: AppBar(
        title: const Text('Borrador Dev'),
        actions: [
          IconButton(
            tooltip: 'Nuevo',
            icon: const Icon(Icons.add),
            onPressed: loading ? null : _nuevo,
          ),
          IconButton(
            tooltip: 'Buscar',
            icon: const Icon(Icons.search),
            onPressed: loading ? null : _buscarNumeroSheet,
          ),
          IconButton(
            tooltip: 'Parámetros',
            icon: const Icon(Icons.settings),
            onPressed: loading ? null : widget.onOpenParametros,
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(24),
          child: Padding(
            padding: const EdgeInsets.only(bottom: 6),
            child: Text(
              "API: ${widget.baseUrl}   |   Usuario: $usuarioFijo",
              style: TextStyle(
                fontSize: 12,
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
          ),
        ),
      ),

      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(10, 8, 10, 10),
          child: LayoutBuilder(
            builder: (context, c) {
              final narrow = c.maxWidth < 520;

              const h = 48.0;
              const gap = 8.0;

              final filledStyle = FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(h),
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              );

              final tonalStyle = FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(h),
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              );

              final outlineStyle = OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(h),
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                side: BorderSide(color: Theme.of(context).colorScheme.outline),
              );

              Widget label(String t) => Text(
                    t,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  );

              final btnGuardar = FilledButton(
                style: filledStyle,
                onPressed: loading ? null : _guardar,
                child: label(loading ? '...' : 'Guardar'),
              );

              final btnAprobar = FilledButton.tonal(
                style: tonalStyle,
                onPressed: loading ? null : _aprobar,
                child: label(loading ? '...' : 'Aprobar'),
              );

              final btnDetalle = OutlinedButton(
                style: outlineStyle,
                onPressed: loading ? null : _abrirDetalle,
                child: label(loading ? '...' : 'Detalle'),
              );

              if (narrow) {
                return Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      children: [
                        Expanded(child: btnGuardar),
                        const SizedBox(width: gap),
                        Expanded(child: btnAprobar),
                      ],
                    ),
                    const SizedBox(height: gap),
                    SizedBox(width: double.infinity, child: btnDetalle),
                  ],
                );
              }

              return Row(
                children: [
                  Expanded(child: btnGuardar),
                  const SizedBox(width: gap),
                  Expanded(child: btnAprobar),
                  const SizedBox(width: gap),
                  Expanded(child: btnDetalle),
                ],
              );
            },
          ),
        ),
      ),

      body: GestureDetector(
        onTap: () => FocusScope.of(context).unfocus(),
        child: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 820),
              child: Padding(
                padding: const EdgeInsets.all(10),
                child: Form(
                  key: _formKey,
                  child: ListView(
                    children: [
                      if (error != null) ...[
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: Colors.red.withOpacity(0.12),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Text(error!, style: const TextStyle(color: Colors.red)),
                        ),
                        const SizedBox(height: 10),
                      ],

                      // CABECERA
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(10),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              _sectionTitle("Cabecera"),

                              if (isWide)
                                Row(
                                  children: [
                                    Expanded(
                                      child: TextFormField(
                                        controller: numeroCtrl,
                                        readOnly: true,
                                        decoration: _dec(
                                          'Número',
                                          suffix: Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              IconButton(
                                                tooltip: 'Buscar',
                                                icon: const Icon(Icons.search),
                                                onPressed: loading ? null : _buscarNumeroSheet,
                                              ),
                                              IconButton(
                                                tooltip: 'Nuevo',
                                                icon: const Icon(Icons.add),
                                                onPressed: loading ? null : _nuevo,
                                              ),
                                            ],
                                          ),
                                        ),
                                      ),
                                    ),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: TextFormField(
                                        controller: fechaCtrl,
                                        decoration: _dec(
                                          'Fecha',
                                          suffix: IconButton(
                                            icon: const Icon(Icons.calendar_month, size: 20),
                                            onPressed: _pickFecha,
                                          ),
                                        ),
                                      ),
                                    ),
                                  ],
                                )
                              else ...[
                                TextFormField(
                                  controller: numeroCtrl,
                                  readOnly: true,
                                  decoration: _dec(
                                    'Número',
                                    suffix: IconButton(
                                      tooltip: 'Buscar',
                                      icon: const Icon(Icons.search),
                                      onPressed: loading ? null : _buscarNumeroSheet,
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 10),
                                TextFormField(
                                  controller: fechaCtrl,
                                  decoration: _dec(
                                    'Fecha',
                                    suffix: IconButton(
                                      icon: const Icon(Icons.calendar_month, size: 20),
                                      onPressed: _pickFecha,
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 8),
                                SizedBox(
                                  width: double.infinity,
                                  child: OutlinedButton.icon(
                                    onPressed: loading ? null : _nuevo,
                                    icon: const Icon(Icons.add),
                                    label: const Text("Nuevo"),
                                  ),
                                ),
                              ],

                              const SizedBox(height: 10),
                              TextFormField(
                                controller: observacionCtrl,
                                decoration: _dec('Observación'),
                              ),

                              const SizedBox(height: 8),
                              Text(
                                numeroCtrl.text.trim().isEmpty
                                    ? "Modo: NUEVO (crea número al Guardar)"
                                    : "Modo: ESCANEAR a #${numeroCtrl.text.trim()} (autoincrementa cantidad)",
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),

                      const SizedBox(height: 10),

                      // ESCANEO
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(10),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              _sectionTitle("Escaneo"),

                              TextFormField(
                                controller: codigoCtrl,
                                focusNode: codigoFocus,
                                decoration: _dec('CodigoBarra'),
                                textInputAction: TextInputAction.done,
                                validator: (v) {
                                  if (!_validarDetalle) return null;
                                  if (v == null || v.trim().isEmpty) return 'Req';
                                  return null;
                                },
                                onFieldSubmitted: (_) {
                                  if (!loading) _guardar();
                                },
                              ),
                              const SizedBox(height: 10),

                              TextFormField(
                                controller: numeroCajaCtrl,
                                keyboardType: TextInputType.number,
                                decoration: _dec('NumeroCaja (default 1)'),
                                validator: _reqInt,
                              ),

                              const SizedBox(height: 8),
                              Text(
                                "Tip: Si escaneas el mismo código otra vez (misma caja), el backend suma +1 automáticamente.",
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),

                      const SizedBox(height: 90),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// BottomSheet de búsqueda: SOLO números existentes (viene de /list)
class _BuscarNumeroSheet extends StatefulWidget {
  final BorradorDevApi api;
  const _BuscarNumeroSheet({required this.api});

  @override
  State<_BuscarNumeroSheet> createState() => _BuscarNumeroSheetState();
}

class _BuscarNumeroSheetState extends State<_BuscarNumeroSheet> {
  final searchCtrl = TextEditingController();
  Timer? _debounce;

  bool loading = true;
  String? error;
  List<Map<String, dynamic>> items = [];

  @override
  void initState() {
    super.initState();
    _load();
    searchCtrl.addListener(_onSearchChange);
  }

  @override
  void dispose() {
    _debounce?.cancel();
    searchCtrl.dispose();
    super.dispose();
  }

  void _onSearchChange() {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), _load);
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      final res = await widget.api.list(q: searchCtrl.text.trim(), top: 50);
      setState(() => items = res);
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final h = MediaQuery.sizeOf(context).height;

    return SizedBox(
      height: h * 0.80,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 12),
        child: Column(
          children: [
            TextField(
              controller: searchCtrl,
              decoration: const InputDecoration(
                labelText: "Buscar (Número/Usuario/Observación)",
                border: OutlineInputBorder(),
                isDense: true,
                prefixIcon: Icon(Icons.search),
              ),
            ),
            const SizedBox(height: 10),

            if (loading)
              const Expanded(child: Center(child: CircularProgressIndicator()))
            else if (error != null)
              Expanded(
                child: Center(
                  child: Text(
                    error!,
                    style: const TextStyle(color: Colors.red),
                    textAlign: TextAlign.center,
                  ),
                ),
              )
            else if (items.isEmpty)
              const Expanded(child: Center(child: Text("Sin resultados.")))
            else
              Expanded(
                child: ListView.separated(
                  itemCount: items.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final it = items[i];
                    final numero = it["Numero"]?.toString() ?? "";
                    final usuario = it["Usuario"]?.toString() ?? "";
                    final obs = it["Observacion"]?.toString() ?? "";
                    final fecha = it["Fecha"]?.toString() ?? "";
                    final status = it["Status"]?.toString() ?? "";

                    return ListTile(
                      title: Text("#$numero"),
                      subtitle: Text("$fecha  |  $usuario  |  Status: $status\n$obs"),
                      isThreeLine: true,
                      onTap: () {
                        final n = int.tryParse(numero);
                        Navigator.pop(context, n);
                      },
                    );
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

