import 'package:flutter/material.dart';
import 'package:borrador_dev_app/services/borrador_dev_api.dart';

class DetalleScreen extends StatefulWidget {
  final BorradorDevApi api;
  final int numero;

  const DetalleScreen({super.key, required this.api, required this.numero});

  @override
  State<DetalleScreen> createState() => _DetalleScreenState();
}

class _DetalleScreenState extends State<DetalleScreen> {
  bool loading = true;
  bool saving = false;
  String? error;

  Map<String, dynamic>? header;
  List<_LineaEdit> lineas = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final l in lineas) {
      l.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      final data = await widget.api.getDetalle(widget.numero);

      if (!mounted) return;

      if (data == null) {
        for (final l in lineas) {
          l.dispose();
        }
        setState(() {
          header = null;
          lineas = [];
          loading = false;
        });
        return;
      }

      final newHeader = (data['header'] as Map).cast<String, dynamic>();
      final dets = (data['detalles'] as List)
          .cast<Map>()
          .map((e) => e.cast<String, dynamic>())
          .toList();

      for (final l in lineas) {
        l.dispose();
      }

      final newLineas = dets.map(_LineaEdit.fromJson).toList()
        ..sort((a, b) {
          final c1 = a.numeroCaja.compareTo(b.numeroCaja);
          if (c1 != 0) return c1;
          return a.codigoBarra.compareTo(b.codigoBarra);
        });

      setState(() {
        header = newHeader;
        lineas = newLineas;
      });
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  dynamic _pickHeader(String camel, String pascal) =>
      (header ?? const {})[camel] ?? (header ?? const {})[pascal];

  int _headerStatus() {
    final v = _pickHeader('status', 'Status');
    if (v is int) return v;
    if (v is num) return v.toInt();
    return int.tryParse(v?.toString() ?? '') ?? 0;
  }

  bool get _bloqueado => header != null && _headerStatus() == 1;

  Future<void> _guardarCambios() async {
    if (_bloqueado) {
      setState(() => error = "Este número está APROBADO (Status=1) y no se puede editar.");
      return;
    }

    setState(() {
      saving = true;
      error = null;
    });

    try {
      if (header == null) {
        setState(() => error = "Ese número no existe.");
        return;
      }

      for (final l in lineas) {
        if (!l.validate()) {
          setState(() => error = "Hay cantidades inválidas. Cantidad debe ser numérica y > 0.");
          return;
        }
      }

      final payload = lineas.map((l) => l.toUpdateJson()).toList();

      await widget.api.updateDetalle(
        numero: widget.numero,
        detalles: payload,
      );

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Guardado con éxito")),
      );

      await _load();
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  InputDecoration _dec(String label) => const InputDecoration(
        border: OutlineInputBorder(),
        isDense: true,
        contentPadding: EdgeInsets.symmetric(horizontal: 10, vertical: 10),
      ).copyWith(labelText: label);

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final isWide = width >= 700;

    final locked = _bloqueado;

    return Scaffold(
      resizeToAvoidBottomInset: true,
      appBar: AppBar(
        title: Text('Detalle #${widget.numero}${locked ? " 🔒" : ""}'),
        actions: [
          IconButton(
            tooltip: 'Recargar',
            icon: const Icon(Icons.refresh),
            onPressed: (loading || saving) ? null : _load,
          ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(10, 8, 10, 10),
          child: FilledButton(
            onPressed: (loading || saving || header == null || locked)
                ? null
                : _guardarCambios,
            child: Text(
              locked ? "Aprobado (solo lectura)" : (saving ? "Guardando..." : "Guardar cambios"),
            ),
          ),
        ),
      ),
      body: GestureDetector(
        onTap: () => FocusScope.of(context).unfocus(),
        child: SafeArea(
          child: loading
              ? const Center(child: CircularProgressIndicator())
              : Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 900),
                    child: Padding(
                      padding: const EdgeInsets.all(10),
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

                          if (header == null) ...[
                            const SizedBox(height: 10),
                            const Text("No existe ese número en la base de datos."),
                          ] else ...[
                            if (locked) ...[
                              Container(
                                width: double.infinity,
                                padding: const EdgeInsets.all(10),
                                decoration: BoxDecoration(
                                  color: Colors.orange.withOpacity(0.14),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: const Text(
                                  "APROBADO (Status=1): este número está bloqueado, solo lectura.",
                                  style: TextStyle(fontWeight: FontWeight.w600),
                                ),
                              ),
                              const SizedBox(height: 10),
                            ],

                            Text(
                              'Líneas (${lineas.length})',
                              style: const TextStyle(fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 8),

                            for (final l in lineas)
                              Card(
                                child: Padding(
                                  padding: const EdgeInsets.all(10),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Wrap(
                                        spacing: 8,
                                        runSpacing: 6,
                                        crossAxisAlignment: WrapCrossAlignment.center,
                                        children: [
                                          const Text('Código:',
                                              style: TextStyle(fontWeight: FontWeight.w600)),
                                          Chip(label: Text(l.codigoBarra)),
                                          const SizedBox(width: 6),
                                          const Text('Caja:',
                                              style: TextStyle(fontWeight: FontWeight.w600)),
                                          Chip(label: Text(l.numeroCaja.toString())),
                                        ],
                                      ),

                                      const SizedBox(height: 8),

                                      // Referencia + Nombre (solo lectura)
                                      Text(
                                        "Referencia: ${l.referencia.isEmpty ? "-" : l.referencia}",
                                        style: const TextStyle(fontWeight: FontWeight.w600),
                                      ),
                                      const SizedBox(height: 4),
                                      Text(
                                        "Nombre: ${l.nombre.isEmpty ? "-" : l.nombre}",
                                      ),

                                      const SizedBox(height: 12),

                                      if (isWide)
                                        Row(
                                          children: [
                                            Expanded(
                                              child: TextField(
                                                controller: l.cantidadCtrl,
                                                decoration: _dec('Cantidad'),
                                                keyboardType: const TextInputType.numberWithOptions(
                                                  decimal: true,
                                                  signed: false,
                                                ),
                                                readOnly: locked,
                                                enabled: !locked,
                                              ),
                                            ),
                                          ],
                                        )
                                      else
                                        TextField(
                                          controller: l.cantidadCtrl,
                                          decoration: _dec('Cantidad'),
                                          keyboardType: const TextInputType.numberWithOptions(
                                            decimal: true,
                                            signed: false,
                                          ),
                                          readOnly: locked,
                                          enabled: !locked,
                                        ),

                                      const SizedBox(height: 6),
                                      Text(
                                        "Solo se puede editar Cantidad.",
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
                        ],
                      ),
                    ),
                  ),
                ),
        ),
      ),
    );
  }
}

class _LineaEdit {
  final String codigoBarra;
  final String referencia;
  final String nombre;

  final int numeroCaja; // solo lectura
  final int numeroCajaOriginal; // para payload (por seguridad)

  final TextEditingController cantidadCtrl;

  _LineaEdit({
    required this.codigoBarra,
    required this.referencia,
    required this.nombre,
    required this.numeroCaja,
    required this.numeroCajaOriginal,
    required this.cantidadCtrl,
  });

  void dispose() {
    cantidadCtrl.dispose();
  }

  static String _norm(String s) => s.trim().replaceAll(',', '.');

  static dynamic _pick(Map<String, dynamic> d, String camel, String pascal) =>
      d[camel] ?? d[pascal];

  factory _LineaEdit.fromJson(Map<String, dynamic> d) {
    final codigo = (_pick(d, 'codigoBarra', 'CodigoBarra') ?? '').toString().trim();

    // Estos dos deben venir del backend (JOIN INVENTARIO):
    final ref = (_pick(d, 'referencia', 'Referencia') ?? '').toString().trim();
    final nom = (_pick(d, 'nombre', 'Nombre') ?? '').toString().trim();

    final caja = _toInt(_pick(d, 'numeroCaja', 'NumeroCaja'));
    final cant = (_pick(d, 'cantidad', 'Cantidad') ?? '').toString();

    return _LineaEdit(
      codigoBarra: codigo,
      referencia: ref,
      nombre: nom,
      numeroCaja: caja,
      numeroCajaOriginal: caja,
      cantidadCtrl: TextEditingController(text: cant),
    );
  }

  bool validate() {
    if (codigoBarra.isEmpty) return false;

    final n = num.tryParse(_norm(cantidadCtrl.text));
    if (n == null) return false;
    if (n <= 0) return false;

    if (numeroCaja <= 0) return false;

    return true;
  }

  Map<String, dynamic> toUpdateJson() {
    return {
      "codigoBarra": codigoBarra,
      "cantidad": num.parse(_norm(cantidadCtrl.text)),
      "numeroCaja": numeroCaja,
      "numeroCajaOriginal": numeroCajaOriginal,
    };
  }

  static int _toInt(dynamic v) {
    if (v is int) return v;
    if (v is num) return v.toInt();
    return int.tryParse(v?.toString() ?? '') ?? 0;
  }
}

